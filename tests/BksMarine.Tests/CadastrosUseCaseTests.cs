using BksMarine.Application.Locations;
using BksMarine.Core.Domain.Locations;
using BksMarine.Core.Domain.Ports;
using Xunit;

namespace BksMarine.Tests;

public sealed class CadastrosUseCaseTests
{
    // ---- Porto ----

    [Fact]
    public async Task CreatePort_valid_succeeds_and_uppercases_code()
    {
        var repo = new FakePortRepository();
        var useCase = new CreatePort(repo);

        var result = await useCase.ExecuteAsync(new CreatePortTransaction("Santos", "san", null, null, null));

        Assert.True(result.IsSuccess);
        Assert.Equal("SAN", result.Value!.Code);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task CreatePort_duplicate_code_fails()
    {
        var repo = new FakePortRepository();
        await repo.AddAsync(NewPort("Santos", "SAN"));
        var useCase = new CreatePort(repo);

        var result = await useCase.ExecuteAsync(new CreatePortTransaction("Outro", "san", null, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal("locations.port.code_duplicate", result.Error!.Code);
    }

    [Fact]
    public async Task CreatePort_missing_name_fails() =>
        Assert.Equal("validation.name", (await new CreatePort(new FakePortRepository())
            .ExecuteAsync(new CreatePortTransaction("", "SAN", null, null, null))).Error!.Code);

    [Fact]
    public async Task CreatePort_missing_code_fails() =>
        Assert.Equal("validation.code", (await new CreatePort(new FakePortRepository())
            .ExecuteAsync(new CreatePortTransaction("Santos", "", null, null, null))).Error!.Code);

    [Fact]
    public async Task UpdatePort_changes_data()
    {
        var repo = new FakePortRepository();
        var port = NewPort("Santos", "SAN");
        await repo.AddAsync(port);
        var useCase = new UpdatePort(repo);

        var result = await useCase.ExecuteAsync(new UpdatePortTransaction(port.Id, "Santos Novos", "SAN", "Av X", null, null));

        Assert.True(result.IsSuccess);
        Assert.Equal("Santos Novos", result.Value!.Name);
        Assert.Equal("Av X", result.Value.Address);
    }

    [Fact]
    public async Task UpdatePort_to_other_ports_code_fails()
    {
        var repo = new FakePortRepository();
        var a = NewPort("A", "AAA");
        var b = NewPort("B", "BBB");
        await repo.AddAsync(a);
        await repo.AddAsync(b);
        var useCase = new UpdatePort(repo);

        var result = await useCase.ExecuteAsync(new UpdatePortTransaction(b.Id, "B", "AAA", null, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal("locations.port.code_duplicate", result.Error!.Code);
    }

    [Fact]
    public async Task UpdatePort_unknown_id_fails() =>
        Assert.Equal("locations.port.not_found", (await new UpdatePort(new FakePortRepository())
            .ExecuteAsync(new UpdatePortTransaction(Guid.NewGuid(), "X", "XXX", null, null, null))).Error!.Code);

    [Fact]
    public async Task DeactivatePort_sets_inactive()
    {
        var repo = new FakePortRepository();
        var port = NewPort("Santos", "SAN");
        await repo.AddAsync(port);
        var useCase = new DeactivatePort(repo);

        var result = await useCase.ExecuteAsync(port.Id);

        Assert.True(result.IsSuccess);
        Assert.False(repo.Single(port.Id).IsActive);
    }

    [Fact]
    public async Task DeactivatePort_twice_fails()
    {
        var repo = new FakePortRepository();
        var port = NewPort("Santos", "SAN");
        await repo.AddAsync(port);
        var useCase = new DeactivatePort(repo);
        await useCase.ExecuteAsync(port.Id);

        var result = await useCase.ExecuteAsync(port.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("locations.port.already_inactive", result.Error!.Code);
    }

    [Fact]
    public async Task ListPorts_filters_active_only()
    {
        var repo = new FakePortRepository();
        await repo.AddAsync(NewPort("Santos", "SAN"));
        await repo.AddAsync(NewPort("Rio", "RIO"));
        await repo.DeactivateAsync(repo.ByCode("RIO").Id);
        var useCase = new ListPorts(repo);

        var active = (await useCase.ExecuteAsync(true)).Value!;
        var all = (await useCase.ExecuteAsync(false)).Value!;

        Assert.Single(active);
        Assert.Equal(2, all.Count);
    }

    // ---- Berço ----

    [Fact]
    public async Task CreateBerth_valid_succeeds()
    {
        var ports = new FakePortRepository();
        var port = NewPort("Santos", "SAN");
        await ports.AddAsync(port);
        var berths = new FakeBerthRepository();
        var useCase = new CreateBerth(berths, ports);

        var result = await useCase.ExecuteAsync(
            new CreateBerthTransaction("Berth 01", port.Id, 300m, 50000m, BerthType.Cargo, null));

        Assert.True(result.IsSuccess);
        Assert.Equal("Berth 01", result.Value!.Name);
        Assert.Equal(300m, result.Value.MaxLoa);
    }

    [Fact]
    public async Task CreateBerth_duplicate_name_in_same_port_fails()
    {
        var ports = new FakePortRepository();
        var port = NewPort("Santos", "SAN");
        await ports.AddAsync(port);
        var berths = new FakeBerthRepository();
        await berths.AddAsync(NewBerth("Berth 01", port.Id));
        var useCase = new CreateBerth(berths, ports);

        var result = await useCase.ExecuteAsync(
            new CreateBerthTransaction("Berth 01", port.Id, null, null, BerthType.Cargo, null));

        Assert.True(result.IsFailure);
        Assert.Equal("locations.berth.name_duplicate", result.Error!.Code);
    }

    [Fact]
    public async Task CreateBerth_same_name_different_port_succeeds()
    {
        var ports = new FakePortRepository();
        var a = NewPort("A", "AAA");
        var b = NewPort("B", "BBB");
        await ports.AddAsync(a);
        await ports.AddAsync(b);
        var berths = new FakeBerthRepository();
        await berths.AddAsync(NewBerth("Berth 01", a.Id));
        var useCase = new CreateBerth(berths, ports);

        var result = await useCase.ExecuteAsync(
            new CreateBerthTransaction("Berth 01", b.Id, null, null, BerthType.Cargo, null));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CreateBerth_unknown_port_fails() =>
        Assert.Equal("locations.port.not_found", (await new CreateBerth(new FakeBerthRepository(), new FakePortRepository())
            .ExecuteAsync(new CreateBerthTransaction("B", Guid.NewGuid(), null, null, BerthType.Cargo, null))).Error!.Code);

    [Fact]
    public async Task CreateBerth_inactive_port_fails()
    {
        var ports = new FakePortRepository();
        var port = NewPort("Santos", "SAN");
        await ports.AddAsync(port);
        await ports.DeactivateAsync(port.Id);
        var useCase = new CreateBerth(new FakeBerthRepository(), ports);

        var result = await useCase.ExecuteAsync(
            new CreateBerthTransaction("B", port.Id, null, null, BerthType.Cargo, null));

        Assert.Equal("locations.port.inactive", result.Error!.Code);
    }

    [Fact]
    public async Task CreateBerth_invalid_loa_fails() =>
        Assert.Equal("validation.max_loa", (await new CreateBerth(new FakeBerthRepository(), new FakePortRepository())
            .ExecuteAsync(new CreateBerthTransaction("B", Guid.NewGuid(), -1m, null, BerthType.Cargo, null))).Error!.Code);

    [Fact]
    public async Task ListBerthsByPort_filters_active()
    {
        var port = Guid.NewGuid();
        var berths = new FakeBerthRepository();
        await berths.AddAsync(NewBerth("B1", port));
        await berths.AddAsync(NewBerth("B2", port));
        await berths.DeactivateAsync(berths.ByName("B2", port).Id);
        var useCase = new ListBerthsByPort(berths);

        var active = (await useCase.ExecuteAsync(port, true)).Value!;
        var all = (await useCase.ExecuteAsync(port, false)).Value!;

        Assert.Single(active);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task UpdateBerth_duplicate_other_berth_fails()
    {
        var port = Guid.NewGuid();
        var berths = new FakeBerthRepository();
        var a = NewBerth("A", port);
        var b = NewBerth("B", port);
        await berths.AddAsync(a);
        await berths.AddAsync(b);
        var useCase = new UpdateBerth(berths);

        var result = await useCase.ExecuteAsync(
            new UpdateBerthTransaction(b.Id, "A", null, null, BerthType.Cargo, null));

        Assert.Equal("locations.berth.name_duplicate", result.Error!.Code);
    }

    // ---- helpers ----

    private static Port NewPort(string name, string code) =>
        new(Guid.NewGuid(), name, new PortCode(code), null, null, null, true);

    private static Berth NewBerth(string name, Guid portId) =>
        new(Guid.NewGuid(), name, portId, null, null, BerthType.Cargo, null, true);

    private sealed class FakePortRepository : IPortRepository
    {
        private readonly List<Port> _ports = new();

        public Port ByCode(string code) => _ports.First(p => p.Code.Value == code.ToUpperInvariant());
        public Port Single(Guid id) => _ports.First(p => p.Id == id);

        public Task<Port?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_ports.FirstOrDefault(p => p.Id == id));

        public Task<Port?> GetByCodeAsync(string code, CancellationToken ct = default) =>
            Task.FromResult(_ports.FirstOrDefault(p => p.Code.Value == code.ToUpperInvariant()));

        public Task<List<Port>> ListAsync(bool activeOnly, CancellationToken ct = default) =>
            Task.FromResult(_ports.Where(p => !activeOnly || p.IsActive).ToList());

        public Task AddAsync(Port port, CancellationToken ct = default)
        {
            _ports.Add(port);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Port port, CancellationToken ct = default)
        {
            _ports.RemoveAll(p => p.Id == port.Id);
            _ports.Add(port);
            return Task.CompletedTask;
        }

        public Task DeactivateAsync(Guid id, CancellationToken ct = default) =>
            UpdateAsync(new Port(id, _ports.First(p => p.Id == id).Name, _ports.First(p => p.Id == id).Code, null, null, null, false), ct);
    }

    private sealed class FakeBerthRepository : IBerthRepository
    {
        private readonly List<Berth> _berths = new();

        public Berth ByName(string name, Guid portId) => _berths.First(b => b.Name == name && b.PortId == portId);

        public Task<Berth?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_berths.FirstOrDefault(b => b.Id == id));

        public Task<Berth?> GetByNameInPortAsync(string name, Guid portId, CancellationToken ct = default) =>
            Task.FromResult(_berths.FirstOrDefault(b => b.Name == name && b.PortId == portId));

        public Task<List<Berth>> ListByPortAsync(Guid portId, bool activeOnly, CancellationToken ct = default) =>
            Task.FromResult(_berths.Where(b => b.PortId == portId && (!activeOnly || b.IsActive)).ToList());

        public Task AddAsync(Berth berth, CancellationToken ct = default)
        {
            _berths.Add(berth);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Berth berth, CancellationToken ct = default)
        {
            _berths.RemoveAll(b => b.Id == berth.Id);
            _berths.Add(berth);
            return Task.CompletedTask;
        }

        public Task DeactivateAsync(Guid id, CancellationToken ct = default)
        {
            var b = _berths.First(x => x.Id == id);
            return UpdateAsync(new Berth(b.Id, b.Name, b.PortId, b.MaxLoa, b.MaxDwt, b.Type, b.Notes, false), ct);
        }
    }
}
