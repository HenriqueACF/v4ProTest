using BksMarine.Application.Common;
using BksMarine.Application.Operations;
using BksMarine.Core.Domain.Locations;
using BksMarine.Core.Domain.Operations;
using BksMarine.Core.Domain.Ports;
using BksMarine.Core.Domain.Profiles;
using BksMarine.Core.Domain.Users;
using Xunit;

namespace BksMarine.Tests;

public sealed class ShipAndDetailTests
{
    // ---- Unicidade de nome de navio ----

    [Fact]
    public async Task CreateShip_duplicate_name_fails()
    {
        var existing = new Ship(Guid.NewGuid(), "Pomone", 128m, 5000m, true);
        var useCase = new CreateShip(new FakeShipRepository(existing));

        var result = await useCase.ExecuteAsync(new CreateShipTransaction("Pomone", 100m, 2000m));

        Assert.True(result.IsFailure);
        Assert.Equal("operations.ship_name_duplicate", result.Error!.Code);
    }

    [Fact]
    public async Task UpdateShip_to_other_ships_name_fails()
    {
        var a = new Ship(Guid.NewGuid(), "Pomone", 128m, 5000m, true);
        var b = new Ship(Guid.NewGuid(), "Grande", 240m, 45000m, true);
        var useCase = new UpdateShip(new FakeShipRepository(a, b));

        var result = await useCase.ExecuteAsync(new UpdateShipTransaction(b.Id, "Pomone", 100m, 2000m));

        Assert.True(result.IsFailure);
        Assert.Equal("operations.ship_name_duplicate", result.Error!.Code);
    }

    [Fact]
    public async Task UpdateShip_keeps_own_name_succeeds()
    {
        var a = new Ship(Guid.NewGuid(), "Pomone", 128m, 5000m, true);
        var useCase = new UpdateShip(new FakeShipRepository(a));

        var result = await useCase.ExecuteAsync(new UpdateShipTransaction(a.Id, "Pomone", 130m, 5200m));

        Assert.True(result.IsSuccess);
        Assert.Equal(130m, result.Value!.Loa);
    }

    // ---- Detalhe enriquecido ----

    [Fact]
    public async Task GetOperationDetail_resolves_names()
    {
        var op = BuildOperation();
        var users = new FakeUserRepository(NewUser(op.ResponsibleUserId!.Value, "Ana"));
        var useCase = new GetOperationDetail(
            new FakeOperationRepository(op),
            new FakeShipRepository(new Ship(op.ShipId, "Pomone", 128m, 5000m, true)),
            new FakePortRepository(op.PortId),
            new FakeBerthRepository(op.BerthId, op.PortId),
            users);

        var result = await useCase.ExecuteAsync(op.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("Pomone", result.Value!.ShipName);
        Assert.Equal("Santos", result.Value.PortName);
        Assert.Equal("B1", result.Value.BerthName);
        Assert.Equal("Ana", result.Value.ResponsibleName);
    }

    [Fact]
    public async Task GetOperationDetail_unknown_fails()
    {
        var useCase = new GetOperationDetail(
            new FakeOperationRepository(),
            new FakeShipRepository(),
            new FakePortRepository(),
            new FakeBerthRepository(),
            new FakeUserRepository(NewUser(Guid.NewGuid(), "Ana")));

        var result = await useCase.ExecuteAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("operations.not_found", result.Error!.Code);
    }

    // ---- helpers ----

    private static Operation BuildOperation() =>
        new(
            Guid.NewGuid(), OperationType.Docking, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            null, null, null, null, null, null, null, null, null,
            null, null, null, null, null, DateTime.UtcNow, null,
            new List<string>(), TransmissionStatus.NotTransmitted);

    private static User NewUser(Guid id, string name)
    {
        var profile = new Profile(Guid.NewGuid(), ProfileName.Full, new[] { Module.Configuration });
        return new User(id, name, null, new Email("a@b.com"), new PasswordHash("h"), profile.Id, true);
    }

    private sealed class FakeShipRepository : IShipRepository
    {
        private readonly List<Ship> _ships;

        public FakeShipRepository(params Ship[] ships) => _ships = ships.ToList();

        public Task<Ship?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_ships.FirstOrDefault(s => s.Id == id));

        public Task<Ship?> GetByNameAsync(string name, CancellationToken ct = default) =>
            Task.FromResult(_ships.FirstOrDefault(s => s.Name == name));

        public Task<List<Ship>> ListAsync(bool activeOnly, CancellationToken ct = default) => Task.FromResult(_ships);
        public Task AddAsync(Ship ship, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Ship ship, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeOperationRepository : IOperationRepository
    {
        private readonly Operation? _operation;

        public FakeOperationRepository(Operation? operation = null) => _operation = operation;

        public Task<Operation?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_operation is not null && _operation.Id == id ? _operation : null);

        public Task<List<Operation>> ListAsync(OperationType? type, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default) => Task.FromResult(new List<Operation>());
        public Task<int> CountAsync(OperationType? type, DateTime? from, DateTime? to, CancellationToken ct = default) => Task.FromResult(0);
        public Task<List<OperationReportRow>> ListReportAsync(OperationType? type, DateTime? from, DateTime? to, Guid? portId, Guid? responsibleUserId, CancellationToken ct = default) => Task.FromResult(new List<OperationReportRow>());
        public Task AddAsync(Operation operation, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Operation operation, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakePortRepository : IPortRepository
    {
        private readonly Port? _port;

        public FakePortRepository(Guid? portId = null) => _port = portId is null ? null : new Port(portId.Value, "Santos", new PortCode("SAN"), null, null, null, true);

        public Task<Port?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_port is not null && _port.Id == id ? _port : null);

        public Task<Port?> GetByCodeAsync(string code, CancellationToken ct = default) => Task.FromResult<Port?>(null);
        public Task<List<Port>> ListAsync(bool activeOnly, int page, int pageSize, CancellationToken ct = default) => Task.FromResult(new List<Port>());
        public Task<int> CountAsync(bool activeOnly, CancellationToken ct = default) => Task.FromResult(0);
        public Task AddAsync(Port port, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Port port, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeBerthRepository : IBerthRepository
    {
        private readonly Berth? _berth;

        public FakeBerthRepository(Guid? berthId = null, Guid? portId = null) =>
            _berth = berthId is null ? null : new Berth(berthId.Value, "B1", portId!.Value, null, null, BerthType.Cargo, null, true);

        public Task<Berth?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_berth is not null && _berth.Id == id ? _berth : null);

        public Task<Berth?> GetByNameInPortAsync(string name, Guid portId, CancellationToken ct = default) => Task.FromResult<Berth?>(null);
        public Task<List<Berth>> ListByPortAsync(Guid portId, bool activeOnly, int page, int pageSize, CancellationToken ct = default) => Task.FromResult(new List<Berth>());
        public Task<int> CountByPortAsync(Guid portId, bool activeOnly, CancellationToken ct = default) => Task.FromResult(0);
        public Task AddAsync(Berth berth, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Berth berth, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly UserAccount _account;

        public FakeUserRepository(User user)
        {
            var profile = new Profile(user.ProfileId, ProfileName.Full, new[] { Module.Configuration });
            _account = new UserAccount(user, profile);
        }

        public Task<UserAccount?> GetByEmailAsync(Email email, CancellationToken ct = default) => Task.FromResult<UserAccount?>(_account);
        public Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<UserAccount?>(_account.User.Id == id ? _account : null);
        public Task<List<UserAccount>> ListAsync(bool activeOnly, int page, int pageSize, CancellationToken ct = default) => Task.FromResult(new List<UserAccount>());
        public Task<int> CountAsync(bool activeOnly, CancellationToken ct = default) => Task.FromResult(0);
        public Task AddAsync(User user, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(User user, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdatePasswordAsync(Guid userId, PasswordHash hash, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Profile?> GetProfileByIdAsync(Guid profileId, CancellationToken ct = default) => Task.FromResult<Profile?>(_account.Profile);
        public Task<List<Profile>> GetAllProfilesAsync(CancellationToken ct = default) => Task.FromResult(new List<Profile>());
    }
}
