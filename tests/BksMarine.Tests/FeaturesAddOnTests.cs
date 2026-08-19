using BksMarine.Application.Auth;
using BksMarine.Application.Common;
using BksMarine.Application.Operations;
using BksMarine.Core.Domain.Locations;
using BksMarine.Core.Domain.Operations;
using BksMarine.Core.Domain.Ports;
using BksMarine.Core.Domain.Profiles;
using BksMarine.Core.Domain.Users;
using Xunit;

namespace BksMarine.Tests;

public sealed class FeaturesAddOnTests
{
    // ---- Paginação ----

    [Theory]
    [InlineData(null, null, 1, 10)]
    [InlineData(0, 0, 1, 10)]
    [InlineData(3, 5, 3, 5)]
    [InlineData(2, 500, 2, 100)]
    public void Paging_normalizes(int? page, int? pageSize, int expectedPage, int expectedSize)
    {
        var (p, ps) = Paging.Normalize(page, pageSize);
        Assert.Equal(expectedPage, p);
        Assert.Equal(expectedSize, ps);
    }

    [Fact]
    public void PageResult_computes_total_pages()
    {
        var result = new PageResult<int>(new[] { 1, 2, 3 }, 1, 10, 25);
        Assert.Equal(3, result.TotalPages);
    }

    // ---- Reset de senha ----

    [Fact]
    public async Task ResetPassword_changes_hash_with_correct_current()
    {
        var repo = new FakeUserRepository(NewUser("a@b.com", "secret", true));
        var hasher = new FakePasswordHasher();
        var useCase = new ResetPassword(repo, hasher);

        var result = await useCase.ExecuteAsync(repo.ById, new ResetPasswordTransaction("secret", "novaSenha1"));

        Assert.True(result.IsSuccess);
        var stored = (await repo.GetByIdAsync(repo.ById))!.User.PasswordHash;
        Assert.True(hasher.Verify("novaSenha1", stored));
    }

    [Fact]
    public async Task ResetPassword_wrong_current_fails()
    {
        var repo = new FakeUserRepository(NewUser("a@b.com", "secret", true));
        var useCase = new ResetPassword(repo, new FakePasswordHasher());

        var result = await useCase.ExecuteAsync(repo.ById, new ResetPasswordTransaction("errada", "novaSenha1"));

        Assert.True(result.IsFailure);
        Assert.Equal("auth.invalid_credentials", result.Error!.Code);
    }

    [Fact]
    public async Task ResetPassword_short_new_fails()
    {
        var repo = new FakeUserRepository(NewUser("a@b.com", "secret", true));
        var useCase = new ResetPassword(repo, new FakePasswordHasher());

        var result = await useCase.ExecuteAsync(repo.ById, new ResetPasswordTransaction("secret", "abc"));

        Assert.True(result.IsFailure);
        Assert.Equal("validation.new_password", result.Error!.Code);
    }

    [Fact]
    public async Task ResetPassword_inactive_fails()
    {
        var repo = new FakeUserRepository(NewUser("a@b.com", "secret", false));
        var useCase = new ResetPassword(repo, new FakePasswordHasher());

        var result = await useCase.ExecuteAsync(repo.ById, new ResetPasswordTransaction("secret", "novaSenha1"));

        Assert.Equal("auth.invalid_credentials", result.Error!.Code);
    }

    // ---- Funcionário responsável na operação ----

    [Fact]
    public async Task Register_with_unknown_responsible_fails()
    {
        var useCase = Build();
        var txc = Docking() with { ResponsibleUserId = Guid.NewGuid() };

        var result = await useCase.ExecuteAsync(txc);

        Assert.True(result.IsFailure);
        Assert.Equal("operations.responsible_not_found", result.Error!.Code);
    }

    [Fact]
    public async Task Register_with_existing_responsible_succeeds()
    {
        var userId = Guid.NewGuid();
        var users = new FakeUserRepository(NewUser("ana@bks.com", "secret", true, userId));
        var useCase = Build(users: users);
        var txc = Docking() with { ResponsibleUserId = userId };

        var result = await useCase.ExecuteAsync(txc);

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value!.ResponsibleUserId);
    }

    // ---- helpers ----

    private static readonly Guid ShipId = Guid.NewGuid();
    private static readonly Guid PortId = Guid.NewGuid();
    private static readonly Guid BerthId = Guid.NewGuid();

    private static RegisterOperationTransaction Docking() =>
        new(
            OperationType.Docking, ShipId, PortId, BerthId, null,
            null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, DateTime.UtcNow, null,
            new List<string>());

    private static RegisterOperation Build(IUserRepository? users = null) =>
        new(
            new FakeOperationRepository(),
            new FakeShipRepository(),
            new FakePortRepository(),
            new FakeBerthRepository(),
            new FakeStorage(),
            users ?? new FakeUserRepository(NewUser("a@b.com", "secret", true)));

    private static User NewUser(string email, string password, bool active, Guid? id = null)
    {
        var profile = new Profile(Guid.NewGuid(), ProfileName.Full, new[] { Module.Configuration });
        return new User(id ?? Guid.NewGuid(), "Ana", null, new Email(email), new PasswordHash(password + "-h"), profile.Id, active);
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public PasswordHash Hash(string plainPassword) => new(plainPassword + "-h");
        public bool Verify(string plainPassword, PasswordHash hash) => hash.Value == plainPassword + "-h";
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<UserAccount> _accounts;

        public FakeUserRepository(User user)
        {
            var profile = new Profile(user.ProfileId, ProfileName.Full, new[] { Module.Configuration });
            _accounts = new List<UserAccount> { new(user, profile) };
        }

        public Guid ById => _accounts[0].User.Id;

        public Task<UserAccount?> GetByEmailAsync(Email email, CancellationToken ct = default) =>
            Task.FromResult(_accounts.FirstOrDefault(a => a.User.Email.Value == email.Value));

        public Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_accounts.FirstOrDefault(a => a.User.Id == id));

        public Task<List<UserAccount>> ListAsync(bool activeOnly, int page, int pageSize, CancellationToken ct = default) => Task.FromResult(_accounts);
        public Task<int> CountAsync(bool activeOnly, CancellationToken ct = default) => Task.FromResult(_accounts.Count);

        public Task AddAsync(User user, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(User user, CancellationToken ct = default) => Task.CompletedTask;

        public Task UpdatePasswordAsync(Guid userId, PasswordHash hash, CancellationToken ct = default)
        {
            var account = _accounts.First(a => a.User.Id == userId);
            _accounts.Clear();
            var updated = new User(account.User.Id, account.User.Name, account.User.JobTitle, account.User.Email, hash, account.User.ProfileId, account.User.IsActive);
            _accounts.Add(new UserAccount(updated, account.Profile));
            return Task.CompletedTask;
        }

        public Task<Profile?> GetProfileByIdAsync(Guid profileId, CancellationToken ct = default) => Task.FromResult<Profile?>(null);
        public Task<List<Profile>> GetAllProfilesAsync(CancellationToken ct = default) => Task.FromResult(new List<Profile>());
    }

    private sealed class FakeStorage : IStorageClient
    {
        public Task<string> SaveAsync(string base64Content, CancellationToken ct = default) => Task.FromResult("/uploads/f.jpg");
    }

    private sealed class FakeOperationRepository : IOperationRepository
    {
        private readonly List<Operation> _operations = new();
        public Task<Operation?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(_operations.FirstOrDefault(o => o.Id == id));
        public Task<List<Operation>> ListAsync(OperationType? type, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default) => Task.FromResult(_operations);
        public Task<int> CountAsync(OperationType? type, DateTime? from, DateTime? to, CancellationToken ct = default) => Task.FromResult(_operations.Count);
        public Task<List<OperationReportRow>> ListReportAsync(OperationType? type, DateTime? from, DateTime? to, Guid? portId, Guid? responsibleUserId, CancellationToken ct = default) => Task.FromResult(new List<OperationReportRow>());
        public Task AddAsync(Operation operation, CancellationToken ct = default) { _operations.Add(operation); return Task.CompletedTask; }
        public Task UpdateAsync(Operation operation, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeShipRepository : IShipRepository
    {
        public Task<Ship?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult<Ship?>(id == ShipId ? new Ship(id, "Pomone", 128m, 5000m, true) : null);
        public Task<Ship?> GetByNameAsync(string name, CancellationToken ct = default) => Task.FromResult<Ship?>(null);
        public Task<List<Ship>> ListAsync(bool activeOnly, CancellationToken ct = default) => Task.FromResult(new List<Ship>());
        public Task AddAsync(Ship ship, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Ship ship, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakePortRepository : IPortRepository
    {
        public Task<Port?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult<Port?>(id == PortId ? new Port(id, "Santos", new PortCode("SAN"), null, null, null, true) : null);
        public Task<Port?> GetByCodeAsync(string code, CancellationToken ct = default) => Task.FromResult<Port?>(null);
        public Task<List<Port>> ListAsync(bool activeOnly, int page, int pageSize, CancellationToken ct = default) => Task.FromResult(new List<Port>());
        public Task<int> CountAsync(bool activeOnly, CancellationToken ct = default) => Task.FromResult(0);
        public Task AddAsync(Port port, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Port port, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeBerthRepository : IBerthRepository
    {
        public Task<Berth?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult<Berth?>(id == BerthId ? new Berth(id, "B1", PortId, null, null, BerthType.Cargo, null, true) : null);
        public Task<Berth?> GetByNameInPortAsync(string name, Guid portId, CancellationToken ct = default) => Task.FromResult<Berth?>(null);
        public Task<List<Berth>> ListByPortAsync(Guid portId, bool activeOnly, int page, int pageSize, CancellationToken ct = default) => Task.FromResult(new List<Berth>());
        public Task<int> CountByPortAsync(Guid portId, bool activeOnly, CancellationToken ct = default) => Task.FromResult(0);
        public Task AddAsync(Berth berth, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Berth berth, CancellationToken ct = default) => Task.CompletedTask;
    }
}
