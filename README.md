# BKS-Marine — README Técnico

Documentação técnica do backend. Para visão de produto, ver [`README-produto.md`](README-produto.md) e [`HOW-TO-WORK.md`](HOW-TO-WORK.md).

## Stack

| Camada | Tecnologia |
|--------|-----------|
| Runtime | .NET 10 (SDK 10.0.400) |
| API | ASP.NET Core (minimal hosting) |
| Persistência | PostgreSQL (via Supabase) — Npgsql 10.0.3 + Dapper 2.1.79 |
| Autenticação | JWT (Bearer) próprio no .NET + bcrypt (BCrypt.Net-Next 4.2.0) |
| Validação de token | Microsoft.AspNetCore.Authentication.JwtBearer 10.0.11 |
| Relatórios PDF | QuestPDF (Community) |
| Testes | xUnit |

> Supabase entra **apenas** como Postgres gerenciado + Storage. Auth é própria (não usa Supabase Auth).

## Arquitetura — Hexagonal (Ports & Adapters), MultiProject

Fluxo de dependência é unidirecional, do mais externo para o domínio:

```
Api ──▶ Application ──▶ Core ◀── Infrastructure
        (use cases)    (domínio)   (adapters)
```

| Projeto | Responsabilidade | Dependências externas |
|---------|-----------------|----------------------|
| `BksMarine.Core` | Domínio puro: entidades, value objects, enums, **ports** (interfaces) | nenhuma |
| `BksMarine.Application` | Use cases, pipeline, TXC, `Result` | só `Core` |
| `BksMarine.Infrastructure` | Adapters: repositório Dapper, bcrypt, JWT, schema/seed | Dapper, Npgsql, BCrypt, Jwt |
| `BksMarine.Api` | Controllers, composição de DI, configuração, middleware JWT | `Application` + `Infrastructure` |
| `tests/BksMarine.Tests` | Testes unitários do use case (ports fake in-memory) | `Application` + `Core` |

Regra central: **o Core não importa SDK do Supabase nem ORM**. Supabase/Dapper ficam atrás das ports de saída.

## Estrutura de diretórios

```
src/
  BksMarine.slnx
  BksMarine.Core/
    Domain/Users/        User, Email, PasswordHash, UserAccount
    Domain/Profiles/     Profile, ProfileName, Module
    Domain/Locations/    Port, Berth, PortCode, BerthType
    Domain/Operations/   Ship, Operation, OperationType, Side, TransmissionStatus,
                         OperationReportRow, OperationReportData
    Domain/Ports/        IUserRepository, IPasswordHasher, ITokenService,
                         IPortRepository, IBerthRepository, IShipRepository,
                         IOperationRepository, IStorageClient, IReportGenerator
  BksMarine.Application/
    Common/Result.cs     Result, Result<T>, Error
    Auth/                AuthenticateUser, AuthenticateTransaction, AuthenticationResult
    Locations/           CreatePort/Berth, UpdatePort/Berth, DeactivatePort/Berth,
                         ListPorts, ListBerthsByPort, transactions, results
    Employees/           CreateEmployee, UpdateEmployee, DeactivateEmployee,
                         ListEmployees, ListProfiles, transactions, results
    Operations/          CreateShip, UpdateShip, DeactivateShip, ListShips,
                         RegisterOperation, ListOperations, GetOperation, MarkTransmitted
    Reports/             GenerateOperationReport
    DependencyInjection.cs
  BksMarine.Infrastructure/
    Data/                UserRepository, PortRepository, BerthRepository,
                         ShipRepository, OperationRepository
    Auth/                BCryptPasswordHasher, JwtTokenService
    Storage/             LocalStorageClient (dev)
    Reports/             QuestPdfReportGenerator
    Db/                  Schema (DDL + seed), DatabaseInitializer
    DependencyInjection.cs
  BksMarine.Api/
    Controllers/         AuthController, PortsController, BerthsController,
                         EmployeesController, ProfilesController,
                         ShipsController, OperationsController
    Program.cs
    appsettings.json
tests/
  BksMarine.Tests/       AuthenticateUserTests, CadastrosUseCaseTests,
                         FuncionariosUseCaseTests, OperacoesUseCaseTests,
                         RelatoriosUseCaseTests
```

## Bounded Contexts

| BC | Tipo | Estado |
|----|------|--------|
| Identidade & Acesso | Supporting | **implementado** (auth, funcionários, perfis) |
| Cadastros | Supporting | **implementado** (Portos, Berços) |
| Operações Portuárias | CORE | **implementado** (Atracação/Desatracação, fotos, transmissão) |
| Relatórios | Supporting | **implementado** (PDF com filtros) |

## Feature implementada — Autenticação

Fonte de verdade: `specs/features/FEAT-autenticacao.md`.

### Domínio

- **`User`** (aggregate root): `Id` (Guid), `Email` (VO), `PasswordHash` (VO), `ProfileId`, `IsActive`.
- **`Email`** (VO): regex simples, normalizado para lowercase. `Email.IsValid()` valida sem instanciar.
- **`PasswordHash`** (VO): hash bcrypt, nunca exposto em texto puro.
- **`Profile`**: `Id`, `Name` (enum `ProfileName`), `AllowedModules` (coleção de `Module`).
- **`ProfileName`**: `Full` · `Operational` · `Common`.
- **`Module`**: `Configuration` · `Operations` · `Reports`.

### Ports (Core)

```csharp
IUserRepository.GetByEmailAsync(Email email) → UserAccount?
IPasswordHasher.Hash(string) / Verify(string, PasswordHash) → PasswordHash / bool
ITokenService.Issue(User, Profile) → IssuedToken(Token, ExpiresAt)
```

`UserAccount` é o agregado de leitura: `User` + `Profile` (com módulos), resolvido por um único acesso ao banco.

### Use case — `AuthenticateUser`

Pipeline **Standard** explícito, sem MediatR:

1. **Validação** — e-mail válido, senha presente → `Result` com código `validation.*`.
2. **Processamento** — busca usuário por e-mail; verifica `IsActive` e senha via bcrypt. Falha retorna `auth.invalid_credentials` **genérico** (anti-enumeração: não revela se e-mail existe ou se usuário está inativo).
3. **Pós-processamento** — emite JWT, monta menu a partir dos módulos do perfil.

TXC (`AuthenticateTransaction`) é um `record` imutável que flui pela pipeline; a aplicação lê, não muta. Retorno é `Result<AuthenticationResult>` — erro de negócio não vira exception.

### Endpoint

`POST /auth/login`

Request:
```json
{ "email": "admin@bksmarine.com", "password": "Admin@123" }
```

Response `200 OK`:
```json
{
  "token": "<jwt>",
  "expiresAt": "2026-08-20T02:00:00Z",
  "profile": "Full",
  "menu": ["Configuration", "Operations", "Reports"]
}
```

Erros:

| Código | HTTP | Situação |
|--------|------|----------|
| `validation.email` | 400 | e-mail ausente ou em formato inválido |
| `validation.password` | 400 | senha ausente |
| `auth.invalid_credentials` | 401 | e-mail inexistente, senha errada ou usuário inativo (mesma resposta) |

Claims do JWT: `userId`, `email`, `perfil`.

## Feature implementada — Cadastros (Portos/Berços)

Fonte de verdade: `specs/features/FEAT-cadastros.md`.

### Domínio

- **`Port`** (aggregate root): `Id`, `Name`, `Code` (VO `PortCode`, uppercase único), `Address?`, `Contact?`, `Notes?`, `IsActive`.
- **`Berth`**: `Id`, `Name` (único por porto), `PortId` (FK), `MaxLoa?`, `MaxDwt?`, `Type` (enum `BerthType`), `Notes?`, `IsActive`.
- **`BerthType`**: `Cargo` · `Passenger` · `Mixed`.
- Inativação (soft delete): porto/berço **nunca são excluídos** — referenciados por operações futuras.

### Use cases (pipeline Minimal, sem MediatR)

`CreatePort`, `UpdatePort`, `DeactivatePort`, `ListPorts` · `CreateBerth`, `UpdateBerth`, `DeactivateBerth`, `ListBerthsByPort`.
Cada um segue Validação → Processamento (regras de unicidade + FK) → Pós-processamento, com retorno `Result<T>`.

Regras principais:
- Código de porto **único** (case-insensitive, normalizado uppercase); berço **único por porto**.
- `MaxLoa`/`MaxDwt` quando informados devem ser `> 0`.
- Criar berço exige porto existente e **ativo**.

### Endpoints

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| GET | `/ports?activeOnly=true` | autenticado | lista portos |
| POST | `/ports` | Full | cria porto |
| PUT | `/ports/{id}` | Full | atualiza porto |
| DELETE | `/ports/{id}` | Full | inativa porto |
| GET | `/ports/{portId}/berths` | autenticado | lista berços do porto |
| POST | `/ports/{portId}/berths` | Full | cria berço |
| PUT | `/berths/{id}` | Full | atualiza berço |
| DELETE | `/berths/{id}` | Full | inativa berço |

Escrita exige claim `perfil=Full` (policy `configuration`); leitura aceita qualquer token válido.

Erros principais: `validation.*` → 400 · `locations.port.not_found` / `locations.berth.not_found` → 404 ·
`locations.port.code_duplicate` / `locations.berth.name_duplicate` / `locations.port.inactive` → 409.

## Feature implementada — Funcionários e Perfis

Fonte de verdade: `specs/features/FEAT-funcionarios.md`.

- **`User`** estendido: `Name` (obrigatório), `JobTitle?` — além de `Email`/`PasswordHash`/`ProfileId`/`IsActive`.
- **Perfis fixos** (3): atribuição apenas, sem CRUD. `GET /profiles` expõe os 3 com módulos.
- **`CreateEmployee`** cria a credencial de login no mesmo ato (hash bcrypt), exige perfil válido.
- Inativação revoga o acesso (usuário não autentica).

### Endpoints

| Método | Rota | Auth |
|--------|------|------|
| GET | `/employees?activeOnly=true` | autenticado |
| POST | `/employees` | Full |
| PUT | `/employees/{id}` | Full |
| DELETE | `/employees/{id}` | Full |
| GET | `/profiles` | autenticado |

E-mail único → `employees.email_duplicate` (409). `PUT` não altera e-mail/senha (fora de escopo).

## Feature implementada — Operações (CORE)

Fonte de verdade: `specs/features/FEAT-operacoes.md`.

### Domínio

- **`Ship`**: `Name`, `Loa`, `Dwt` (ambos > 0), `IsActive`. Cadastro próprio.
- **`Operation`** (aggregate root): `Type` (Docking/Undocking), `ShipId`, `PortId`, `BerthId`,
  dados da manobra (agência, prático, rebocadores proa/popa, 1º/último cabo, calados proa/meio/popa,
  bordo `Port`/`Starboard`, observações, `OccurredAt`), `UndockingTime?` (só desatracação),
  `Photos` (≤6 URLs), `TransmissionStatus`.
- **`TransmissionStatus`**: `NotTransmitted` → `Transmitted`.

### Regras e invariantes

- Berço deve pertencer ao porto informado; ship/porto/berço devem existir e estar **ativos**.
- 1º cabo < último cabo (se ambos); calados ≥ 0; desatracação exige `undockingTime`.
- Máximo **6 fotos**; falha de upload aborta o registro.

### Pipeline Full

**Validação** (campos, invariantes) → **Enriquecimento** (carregar Ship/Port/Berth e validar) →
**Processamento** (salvar fotos via `IStorageClient`, persistir) → **Pós-processamento** (status inicial).

### Endpoints

| Método | Rota | Auth |
|--------|------|------|
| GET | `/ships?activeOnly=true` | autenticado |
| POST/PUT/DELETE | `/ships` | Full |
| POST | `/operations` | Full |
| GET | `/operations` / `/operations/{id}` | autenticado |
| POST | `/operations/{id}/transmit` | Full |

Fotos: adapter `IStorageClient` — **dev** grava em disco `uploads/`; produção (Supabase Storage) é D6 em aberto.
Transmissão: status simples no MVP (sem fila — D7 em aberto).

## Feature implementada — Relatórios

Fonte de verdade: `specs/features/FEAT-relatorios.md`.

- **`GET /operations/report`** gera PDF (QuestPDF) com filtros `from`/`to`/`type`/`portId`.
- Cabeçalho com filtros, tabela (data, tipo, navio, porto, berço, status) e **fotos embutidas**
  (quando o arquivo local existe; caso contrário, marcador de indisponível).
- Retorna `application/pdf` p/ download.
- `from > to` → `validation.period` (400). Acesso: qualquer perfil autenticado.

## Banco de dados

Schema e seed em [`src/BksMarine.Infrastructure/Db/Schema.cs`](src/BksMarine.Infrastructure/Db/Schema.cs), aplicados de forma idempotente pelo `DatabaseInitializer` na subida (dev).

```sql
profiles        (id uuid PK, name text UNIQUE)
users           (id uuid PK, name text NOT NULL DEFAULT '', job_title text,
                 email text UNIQUE, password_hash text,
                 profile_id uuid FK → profiles, is_active bool DEFAULT TRUE)
profile_modules (profile_id uuid FK → profiles, module text, PK (profile_id, module))
ports           (id uuid PK, name text, code text UNIQUE, address text, contact text,
                 notes text, is_active bool DEFAULT TRUE)
berths          (id uuid PK, name text, port_id uuid FK → ports,
                 max_loa numeric, max_dwt numeric, type text, notes text,
                 is_active bool DEFAULT TRUE, UNIQUE (port_id, name))
ships           (id uuid PK, name text, loa numeric NOT NULL, dwt numeric NOT NULL,
                 is_active bool DEFAULT TRUE)
operations      (id uuid PK, type text, ship_id uuid FK → ships,
                 port_id uuid FK → ports, berth_id uuid FK → berths,
                 agency_name text, pilot_name text, pilot_boarding_time timestamptz,
                 tug_bow_name text, tug_bow_time timestamptz,
                 tug_stern_name text, tug_stern_time timestamptz,
                 first_line_time timestamptz, last_line_time timestamptz,
                 draft_bow numeric, draft_midship numeric, draft_stern numeric,
                 side text, notes text, occurred_at timestamptz NOT NULL,
                 undocking_time timestamptz, photos text[] DEFAULT '{}',
                 transmission_status text DEFAULT 'NotTransmitted',
                 created_at timestamptz DEFAULT now())
```

Seed estático: 3 perfis + mapeamento de módulos (`Full` → 3 módulos, `Operational` → 2, `Common` → 1).
Seed dinâmico: se `users` estiver vazia, cria um administrador (perfil `Full`) com credenciais do config.

## Configuração

Via `appsettings.json` ou variável de ambiente (override padrão do ASP.NET).

| Chave | Env var | Padrão | Descrição |
|-------|---------|--------|-----------|
| `ConnectionStrings:Postgres` | `ConnectionStrings__Postgres` | `Host=localhost;Port=5432;Database=bksmarine;...` | string de conexão (usar pooler Supavisor no Supabase) |
| `Jwt:Issuer` | `Jwt__Issuer` | `bks-marine` | emissor do token |
| `Jwt:Audience` | `Jwt__Audience` | `bks-marine-api` | audiência do token |
| `Jwt:SigningKey` | `Jwt__SigningKey` | chave dev | chave de assinatura HMAC-SHA256 (≥ 32 bytes) |
| `Jwt:ExpirationMinutes` | `Jwt__ExpirationMinutes` | `480` (8h) | expiração do token |
| `SeedAdmin:Email` | `SeedAdmin__Email` | `admin@bksmarine.com` | e-mail do admin seed |
| `SeedAdmin:Password` | `SeedAdmin__Password` | `Admin@123` | senha do admin seed |
| `Database:InitializeOnStartup` | `Database__InitializeOnStartup` | `true` | aplica schema + seed na subida |

## Como rodar

```bash
# build
dotnet build src/BksMarine.slnx

# testes (61 unitários — ports fake in-memory, sem banco)
dotnet test src/BksMarine.slnx

# executar (precisa de Postgres/Supabase acessível)
dotnet run --project src/BksMarine.Api
```

Credencial do Supabase/Postgres via env var:
```bash
export ConnectionStrings__Postgres="Host=...;Port=...;Database=...;Username=...;Password=..."
```

## Decisões técnicas (resumo ADR)

| ADR | Decisão | Status |
|-----|---------|--------|
| 0001 | Stack base: **.NET 10 + Supabase** (Postgres + Storage) | aceito |
| 0002 | **Auth própria no .NET** (JWT + bcrypt), Supabase só Postgres/Storage | aceito |
| 0003 | Acesso a dados: **Dapper** | aceito |
| 0004 | Estrutura **MultiProject** (Core/Application/Infrastructure/Api) | aceito |
| 0005 | Idioma do código: **inglês** | aceito |
| D8 | Relatórios PDF: **QuestPDF** (Community) | aceito |
| D5–D7, D9–D12 | RLS x RBAC, fotos Storage (produção), transmissão (fila), escopo repo, multi-tenant, refresh token, menu no login vs `/me` | em aberto |

> ADRs em [`decisions/`](decisions/). Detalhamento das abertas em [`docs/design/decisoes-abertas.md`](docs/design/decisoes-abertas.md).

## Testes

**61 testes** (`tests/BksMarine.Tests/`, ports fake in-memory + gerador PDF real, sem banco):

- `AuthenticateUserTests` — login, menu por perfil, credenciais inválidas genéricas, validação.
- `CadastrosUseCaseTests` — CRUD porto/berço, unicidade (código/nome), inativação, capacidade.
- `FuncionariosUseCaseTests` — criação (com email duplicado), hash de senha, perfis, inativação.
- `OperacoesUseCaseTests` — registro de atracação/desatracação, invariantes (berço do porto,
  horários de cabo, calados, fotos ≤6, storage falha), transmissão idempotente.
- `RelatoriosUseCaseTests` — filtros repassados, período inválido, PDF válido (QuestPDF real).

## Próximos passos

1. Conectar Postgres/Supabase real e validar o fluxo completo ponta a ponta (login → cadastros → operação → relatório).
2. Fechar decisões em aberto: D6 (Supabase Storage em produção), D7 (transmissão com fila).
3. Transmissão efetiva p/ sistema externo e compartilhamento de relatórios (HU08 além do status).
