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
    Domain/Ports/        IUserRepository, IPasswordHasher, ITokenService,
                         IPortRepository, IBerthRepository, IssuedToken
  BksMarine.Application/
    Common/Result.cs     Result, Result<T>, Error
    Auth/                AuthenticateUser, AuthenticateTransaction, AuthenticationResult
    Locations/           CreatePort/Berth, UpdatePort/Berth, DeactivatePort/Berth,
                         ListPorts, ListBerthsByPort, transactions, results
    DependencyInjection.cs
  BksMarine.Infrastructure/
    Data/                UserRepository, PortRepository, BerthRepository
    Auth/                BCryptPasswordHasher, JwtTokenService
    Db/                  Schema (DDL + seed), DatabaseInitializer
    DependencyInjection.cs
  BksMarine.Api/
    Controllers/         AuthController, PortsController, BerthsController
    Program.cs
    appsettings.json
tests/
  BksMarine.Tests/       AuthenticateUserTests, CadastrosUseCaseTests
```

## Bounded Contexts

| BC | Tipo | Estado |
|----|------|--------|
| Identidade & Acesso | Supporting | **implementado** (auth) |
| Cadastros | Supporting | **implementado** (Portos, Berços) |
| Operações Portuárias | CORE | pendente (Atracação/Desatracação) |
| Relatórios | Supporting | pendente (PDF, filtros) |

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

## Banco de dados

Schema e seed em [`src/BksMarine.Infrastructure/Db/Schema.cs`](src/BksMarine.Infrastructure/Db/Schema.cs), aplicados de forma idempotente pelo `DatabaseInitializer` na subida (dev).

```sql
profiles        (id uuid PK, name text UNIQUE)
users           (id uuid PK, email text UNIQUE, password_hash text,
                 profile_id uuid FK → profiles, is_active bool DEFAULT TRUE)
profile_modules (profile_id uuid FK → profiles, module text, PK (profile_id, module))
ports           (id uuid PK, name text, code text UNIQUE, address text, contact text,
                 notes text, is_active bool DEFAULT TRUE)
berths          (id uuid PK, name text, port_id uuid FK → ports,
                 max_loa numeric, max_dwt numeric, type text, notes text,
                 is_active bool DEFAULT TRUE, UNIQUE (port_id, name))
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

# testes (27 unitários — ports fake in-memory, sem banco)
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
| D5–D12 | RLS x RBAC, fotos Storage, transmissão, PDF, escopo repo, multi-tenant, refresh token, menu no login vs `/me` | em aberto |

> ADRs em [`decisions/`](decisions/). Detalhamento das abertas em [`docs/design/decisoes-abertas.md`](docs/design/decisoes-abertas.md).

## Testes

**27 testes** (`tests/BksMarine.Tests/`, ports fake in-memory, sem banco):

`AuthenticateUserTests` — critérios do FEAT-autenticacao:
- login válido → token + menu correto por perfil (`Full`/`Operational`/`Common`)
- senha errada / e-mail inexistente / usuário inativo → `auth.invalid_credentials` (genérico)
- e-mail inválido → `validation.email`; senha ausente → `validation.password`

`CadastrosUseCaseTests` — critérios do FEAT-cadastros:
- criação/atualização de porto; código duplicado; inativação; lista com `activeOnly`
- criação de berço; nome duplicado (mesmo porto vs porto diferente); porto inexistente/inativo;
  capacidade inválida; lista por porto

## Próximos passos

1. Conectar Postgres/Supabase real e validar login + cadastros de ponta a ponta.
2. Implementar **Operações (CORE)** — atracação/desatracação.
3. Fechar decisões em aberto D5–D12 antes das features que as envolvem (fotos, transmissão, relatórios).
