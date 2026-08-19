using BksMarine.Application.Common;
using BksMarine.Application.Operations;
using BksMarine.Core.Domain.Locations;
using BksMarine.Core.Domain.Operations;
using BksMarine.Core.Domain.Ports;
using BksMarine.Core.Domain.Profiles;
using BksMarine.Core.Domain.Users;
using Xunit;

namespace BksMarine.Tests;

public sealed class OperacoesUseCaseTests
{
    private static readonly Guid ShipId = Guid.NewGuid();
    private static readonly Guid PortId = Guid.NewGuid();
    private static readonly Guid BerthId = Guid.NewGuid();

    // ---- Ship ----

    [Fact]
    public async Task CreateShip_valid_succeeds()
    {
        var repo = new FakeShipRepository();
        var result = await new CreateShip(repo).ExecuteAsync(new CreateShipTransaction("Pomone", 128m, 5000m));
        Assert.True(result.IsSuccess);
        Assert.Equal("Pomone", result.Value!.Name);
    }

    [Fact]
    public async Task CreateShip_invalid_loa_fails() =>
        Assert.Equal("validation.loa", (await new CreateShip(new FakeShipRepository())
            .ExecuteAsync(new CreateShipTransaction("X", 0m, 5m))).Error!.Code);

    [Fact]
    public async Task CreateShip_invalid_dwt_fails() =>
        Assert.Equal("validation.dwt", (await new CreateShip(new FakeShipRepository())
            .ExecuteAsync(new CreateShipTransaction("X", 10m, -1m))).Error!.Code);

    [Fact]
    public async Task UpdateShip_unknown_fails() =>
        Assert.Equal("operations.ship_not_found", (await new UpdateShip(new FakeShipRepository())
            .ExecuteAsync(new UpdateShipTransaction(Guid.NewGuid(), "X", 10m, 5m))).Error!.Code);

    // ---- RegisterOperation ----

    private static RegisterOperationTransaction Docking() =>
        new(
            OperationType.Docking, ShipId, PortId, BerthId, null,
            "Agência X", "Piloto A", DateTime.UtcNow.AddHours(-2),
            "Tug Proa", DateTime.UtcNow.AddHours(-1), "Tug Popa", DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddHours(-1), DateTime.UtcNow,
            12m, 12m, 13m, Side.Port, "obs", DateTime.UtcNow, null,
            new List<string> { "Zm9v" });

    [Fact]
    public async Task Register_docking_succeeds_and_not_transmitted()
    {
        var useCase = Build();
        var result = await useCase.ExecuteAsync(Docking());

        Assert.True(result.IsSuccess);
        Assert.Equal(TransmissionStatus.NotTransmitted, result.Value!.TransmissionStatus);
        Assert.Single(result.Value.Photos);
        Assert.Contains("uploads", result.Value.Photos[0]);
    }

    [Fact]
    public async Task Register_berth_of_other_port_fails()
    {
        var ports = new FakePortRepository(NewPort(PortId), NewPort(Guid.NewGuid()));
        var berths = new FakeBerthRepository(NewBerth(BerthId, Guid.NewGuid()));
        var useCase = Build(ports: ports, berths: berths);

        var result = await useCase.ExecuteAsync(Docking());

        Assert.Equal("operations.berth_not_in_port", result.Error!.Code);
    }

    [Fact]
    public async Task Register_inactive_ship_fails()
    {
        var ships = new FakeShipRepository(NewShip(inactive: true));
        var useCase = Build(ships: ships);

        var result = await useCase.ExecuteAsync(Docking());

        Assert.Equal("operations.ship_inactive", result.Error!.Code);
    }

    [Fact]
    public async Task Register_unknown_port_fails()
    {
        var ports = new FakePortRepository();
        var useCase = Build(ports: ports);

        var result = await useCase.ExecuteAsync(Docking());

        Assert.Equal("operations.port_not_found", result.Error!.Code);
    }

    [Fact]
    public async Task Register_invalid_line_times_fails()
    {
        var txc = Docking() with { FirstLineTime = DateTime.UtcNow, LastLineTime = DateTime.UtcNow.AddHours(-1) };
        var result = await Build().ExecuteAsync(txc);
        Assert.Equal("operations.invalid_line_times", result.Error!.Code);
    }

    [Fact]
    public async Task Register_negative_draft_fails()
    {
        var txc = Docking() with { DraftBow = -1m };
        var result = await Build().ExecuteAsync(txc);
        Assert.Equal("validation.draft_bow", result.Error!.Code);
    }

    [Fact]
    public async Task Register_undocking_without_time_fails()
    {
        var txc = Docking() with { Type = OperationType.Undocking, UndockingTime = null };
        var result = await Build().ExecuteAsync(txc);
        Assert.Equal("validation.undocking_time", result.Error!.Code);
    }

    [Fact]
    public async Task Register_undocking_with_time_succeeds()
    {
        var txc = Docking() with { Type = OperationType.Undocking, UndockingTime = DateTime.UtcNow };
        var result = await Build().ExecuteAsync(txc);
        Assert.True(result.IsSuccess);
        Assert.Equal(OperationType.Undocking, result.Value!.Type);
    }

    [Fact]
    public async Task Register_too_many_photos_fails()
    {
        var txc = Docking() with { Photos = Enumerable.Range(0, 7).Select(_ => "Zm9v").ToList() };
        var result = await Build().ExecuteAsync(txc);
        Assert.Equal("operations.too_many_photos", result.Error!.Code);
    }

    [Fact]
    public async Task Register_storage_failure_fails()
    {
        var useCase = Build(storage: new ThrowingStorage());
        var result = await useCase.ExecuteAsync(Docking());
        Assert.Equal("operations.photo_upload_failed", result.Error!.Code);
    }

    // ---- Transmissão ----

    [Fact]
    public async Task MarkTransmitted_sets_status_and_is_idempotent()
    {
        var repo = new FakeOperationRepository();
        var op = (await Build(repo).ExecuteAsync(Docking())).Value!;
        var useCase = new MarkTransmitted(repo);

        var first = await useCase.ExecuteAsync(op.Id);
        var second = await useCase.ExecuteAsync(op.Id);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(TransmissionStatus.Transmitted, repo.ById(op.Id).TransmissionStatus);
    }

    [Fact]
    public async Task MarkTransmitted_unknown_fails() =>
        Assert.Equal("operations.not_found", (await new MarkTransmitted(new FakeOperationRepository())
            .ExecuteAsync(Guid.NewGuid())).Error!.Code);

    // ---- helpers ----

    private static RegisterOperation Build(
        FakeOperationRepository? operations = null,
        FakeShipRepository? ships = null,
        FakePortRepository? ports = null,
        FakeBerthRepository? berths = null,
        IStorageClient? storage = null,
        IUserRepository? users = null) =>
        new(
            operations ?? new FakeOperationRepository(),
            ships ?? new FakeShipRepository(NewShip()),
            ports ?? new FakePortRepository(NewPort(PortId)),
            berths ?? new FakeBerthRepository(NewBerth(BerthId, PortId)),
            storage ?? new FakeStorage(),
            users ?? new FakeUserRepository());

    private static Ship NewShip(bool inactive = false) => new(ShipId, "Pomone", 128m, 5000m, !inactive);

    private static Port NewPort(Guid id) => new(id, "Santos", new PortCode("SAN"), null, null, null, true);

    private static Berth NewBerth(Guid id, Guid portId) => new(id, "B1", portId, null, null, BerthType.Cargo, null, true);

    private sealed class FakeStorage : IStorageClient
    {
        public Task<string> SaveAsync(string base64Content, CancellationToken ct = default) =>
            Task.FromResult("/uploads/fake.jpg");
    }

    private sealed class ThrowingStorage : IStorageClient
    {
        public Task<string> SaveAsync(string base64Content, CancellationToken ct = default) =>
            throw new InvalidOperationException("storage down");
    }

    private sealed class FakeOperationRepository : IOperationRepository
    {
        private readonly List<Operation> _operations = new();

        public Operation ById(Guid id) => _operations.First(o => o.Id == id);

        public Task<Operation?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_operations.FirstOrDefault(o => o.Id == id));

        public Task<List<Operation>> ListAsync(OperationType? type, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default) =>
            Task.FromResult(_operations.ToList());

        public Task<int> CountAsync(OperationType? type, DateTime? from, DateTime? to, CancellationToken ct = default) =>
            Task.FromResult(_operations.Count);

        public Task<List<OperationReportRow>> ListReportAsync(OperationType? type, DateTime? from, DateTime? to, Guid? portId, Guid? responsibleUserId, CancellationToken ct = default) =>
            Task.FromResult(_operations.Select(o => new OperationReportRow(
                o.Id, o.OccurredAt, o.Type, "Ship", "Port", "Berth", "Responsável", o.TransmissionStatus, o.Photos)).ToList());

        public Task AddAsync(Operation operation, CancellationToken ct = default)
        {
            _operations.Add(operation);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Operation operation, CancellationToken ct = default)
        {
            _operations.RemoveAll(o => o.Id == operation.Id);
            _operations.Add(operation);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeShipRepository : IShipRepository
    {
        private readonly List<Ship> _ships = new();

        public FakeShipRepository(Ship? ship = null)
        {
            if (ship is not null) _ships.Add(ship);
        }

        public Task<Ship?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_ships.FirstOrDefault(s => s.Id == id));

        public Task<Ship?> GetByNameAsync(string name, CancellationToken ct = default) =>
            Task.FromResult(_ships.FirstOrDefault(s => s.Name == name));

        public Task<List<Ship>> ListAsync(bool activeOnly, CancellationToken ct = default) =>
            Task.FromResult(_ships.ToList());

        public Task AddAsync(Ship ship, CancellationToken ct = default)
        {
            _ships.Add(ship);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Ship ship, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakePortRepository : IPortRepository
    {
        private readonly List<Port> _ports;

        public FakePortRepository(params Port[] ports) => _ports = ports.ToList();

        public Task<Port?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_ports.FirstOrDefault(p => p.Id == id));

        public Task<Port?> GetByCodeAsync(string code, CancellationToken ct = default) => Task.FromResult<Port?>(null);
        public Task<List<Port>> ListAsync(bool activeOnly, int page, int pageSize, CancellationToken ct = default) => Task.FromResult(_ports);
        public Task<int> CountAsync(bool activeOnly, CancellationToken ct = default) => Task.FromResult(_ports.Count);
        public Task AddAsync(Port port, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Port port, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeBerthRepository : IBerthRepository
    {
        private readonly List<Berth> _berths;

        public FakeBerthRepository(params Berth[] berths) => _berths = berths.ToList();

        public Task<Berth?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_berths.FirstOrDefault(b => b.Id == id));

        public Task<Berth?> GetByNameInPortAsync(string name, Guid portId, CancellationToken ct = default) =>
            Task.FromResult<Berth?>(null);

        public Task<List<Berth>> ListByPortAsync(Guid portId, bool activeOnly, int page, int pageSize, CancellationToken ct = default) =>
            Task.FromResult(_berths.ToList());

        public Task<int> CountByPortAsync(Guid portId, bool activeOnly, CancellationToken ct = default) =>
            Task.FromResult(_berths.Count);

        public Task AddAsync(Berth berth, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Berth berth, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public Task<UserAccount?> GetByEmailAsync(Email email, CancellationToken ct = default) => Task.FromResult<UserAccount?>(null);
        public Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<UserAccount?>(null);
        public Task<List<UserAccount>> ListAsync(bool activeOnly, int page, int pageSize, CancellationToken ct = default) => Task.FromResult(new List<UserAccount>());
        public Task<int> CountAsync(bool activeOnly, CancellationToken ct = default) => Task.FromResult(0);
        public Task AddAsync(User user, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(User user, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdatePasswordAsync(Guid userId, PasswordHash hash, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Profile?> GetProfileByIdAsync(Guid profileId, CancellationToken ct = default) => Task.FromResult<Profile?>(null);
        public Task<List<Profile>> GetAllProfilesAsync(CancellationToken ct = default) => Task.FromResult(new List<Profile>());
    }
}
