using BksMarine.Application.Employees;
using BksMarine.Core.Domain.Ports;
using BksMarine.Core.Domain.Profiles;
using BksMarine.Core.Domain.Users;
using Xunit;

namespace BksMarine.Tests;

public sealed class FuncionariosUseCaseTests
{
    private static readonly Profile Full = new(Guid.NewGuid(), ProfileName.Full, new[] { Module.Configuration, Module.Operations, Module.Reports });
    private static readonly Profile Operational = new(Guid.NewGuid(), ProfileName.Operational, new[] { Module.Operations, Module.Reports });
    private static readonly Profile Common = new(Guid.NewGuid(), ProfileName.Common, new[] { Module.Reports });

    [Fact]
    public async Task CreateEmployee_valid_succeeds()
    {
        var repo = new FakeUserRepository();
        var useCase = new CreateEmployee(repo, new FakePasswordHasher());

        var result = await useCase.ExecuteAsync(
            new CreateEmployeeTransaction("Ana", "ana@bksmarine.com", "Senha@123", Operational.Id, "Líder de Manobra"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Ana", result.Value!.Name);
        Assert.Equal(ProfileName.Operational, result.Value.Profile);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task CreateEmployee_duplicate_email_fails()
    {
        var repo = new FakeUserRepository();
        await repo.AddAsync(NewUser("ana@bksmarine.com", Full));
        var useCase = new CreateEmployee(repo, new FakePasswordHasher());

        var result = await useCase.ExecuteAsync(
            new CreateEmployeeTransaction("Outra", "ANA@bksmarine.com", "x", Full.Id, null));

        Assert.True(result.IsFailure);
        Assert.Equal("employees.email_duplicate", result.Error!.Code);
    }

    [Fact]
    public async Task CreateEmployee_missing_name_fails() =>
        Assert.Equal("validation.name", (await new CreateEmployee(new FakeUserRepository(), new FakePasswordHasher())
            .ExecuteAsync(new CreateEmployeeTransaction("", "a@b.com", "x", Full.Id, null))).Error!.Code);

    [Fact]
    public async Task CreateEmployee_invalid_email_fails() =>
        Assert.Equal("validation.email", (await new CreateEmployee(new FakeUserRepository(), new FakePasswordHasher())
            .ExecuteAsync(new CreateEmployeeTransaction("Ana", "not-an-email", "x", Full.Id, null))).Error!.Code);

    [Fact]
    public async Task CreateEmployee_missing_password_fails() =>
        Assert.Equal("validation.password", (await new CreateEmployee(new FakeUserRepository(), new FakePasswordHasher())
            .ExecuteAsync(new CreateEmployeeTransaction("Ana", "a@b.com", "", Full.Id, null))).Error!.Code);

    [Fact]
    public async Task CreateEmployee_unknown_profile_fails()
    {
        var repo = new FakeUserRepository();
        var result = await new CreateEmployee(repo, new FakePasswordHasher()).ExecuteAsync(
            new CreateEmployeeTransaction("Ana", "a@b.com", "x", Guid.NewGuid(), null));
        Assert.Equal("employees.profile_not_found", result.Error!.Code);
    }

    [Fact]
    public async Task CreateEmployee_hashes_password()
    {
        var repo = new FakeUserRepository();
        var hasher = new FakePasswordHasher();
        await new CreateEmployee(repo, hasher).ExecuteAsync(
            new CreateEmployeeTransaction("Ana", "a@b.com", "Senha@123", Full.Id, null));

        var stored = (await repo.GetByEmailAsync(new Email("a@b.com")))!;
        Assert.NotEqual("Senha@123", stored.User.PasswordHash.Value);
        Assert.True(hasher.Verify("Senha@123", stored.User.PasswordHash));
    }

    [Fact]
    public async Task UpdateEmployee_changes_name_jobtitle_profile()
    {
        var repo = new FakeUserRepository();
        var user = NewUser("a@b.com", Full);
        await repo.AddAsync(user);
        var useCase = new UpdateEmployee(repo);

        var result = await useCase.ExecuteAsync(new UpdateEmployeeTransaction(user.Id, "Ana Nova", Operational.Id, "Capitã"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Ana Nova", result.Value!.Name);
        Assert.Equal(ProfileName.Operational, result.Value.Profile);
    }

    [Fact]
    public async Task UpdateEmployee_unknown_fails() =>
        Assert.Equal("employees.not_found", (await new UpdateEmployee(new FakeUserRepository())
            .ExecuteAsync(new UpdateEmployeeTransaction(Guid.NewGuid(), "X", Full.Id, null))).Error!.Code);

    [Fact]
    public async Task DeactivateEmployee_sets_inactive()
    {
        var repo = new FakeUserRepository();
        var user = NewUser("a@b.com", Full);
        await repo.AddAsync(user);
        var useCase = new DeactivateEmployee(repo);

        var result = await useCase.ExecuteAsync(user.Id);

        Assert.True(result.IsSuccess);
        Assert.False((await repo.GetByIdAsync(user.Id))!.User.IsActive);
    }

    [Fact]
    public async Task DeactivateEmployee_twice_fails()
    {
        var repo = new FakeUserRepository();
        var user = NewUser("a@b.com", Full);
        await repo.AddAsync(user);
        var useCase = new DeactivateEmployee(repo);
        await useCase.ExecuteAsync(user.Id);

        var result = await useCase.ExecuteAsync(user.Id);

        Assert.Equal("employees.already_inactive", result.Error!.Code);
    }

    [Fact]
    public async Task ListEmployees_filters_active()
    {
        var repo = new FakeUserRepository();
        var a = NewUser("a@b.com", Full);
        var b = NewUser("b@b.com", Common);
        await repo.AddAsync(a);
        await repo.AddAsync(b);
        await repo.UpdateAsync(new User(b.Id, b.Name, b.JobTitle, b.Email, b.PasswordHash, b.ProfileId, false));
        var useCase = new ListEmployees(repo);

        var active = (await useCase.ExecuteAsync(true)).Value!;
        var all = (await useCase.ExecuteAsync(false)).Value!;

        Assert.Single(active.Items);
        Assert.Equal(2, all.Items.Count);
    }

    [Fact]
    public async Task ListProfiles_returns_three()
    {
        var repo = new FakeUserRepository();
        var result = await new ListProfiles(repo).ExecuteAsync();

        Assert.Equal(3, result.Value!.Count);
        var common = result.Value.Single(p => p.Name == ProfileName.Common);
        Assert.Single(common.AllowedModules);
    }

    private static User NewUser(string email, Profile profile) =>
        new(Guid.NewGuid(), "Nome", null, new Email(email), new PasswordHash("hash"), profile.Id, true);

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public PasswordHash Hash(string plainPassword) => new(plainPassword + "-hashed");
        public bool Verify(string plainPassword, PasswordHash hash) => hash.Value == plainPassword + "-hashed";
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<UserAccount> _accounts = new();

        public Task<UserAccount?> GetByEmailAsync(Email email, CancellationToken ct = default) =>
            Task.FromResult(_accounts.FirstOrDefault(a => a.User.Email.Value == email.Value));

        public Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_accounts.FirstOrDefault(a => a.User.Id == id));

        public Task<List<UserAccount>> ListAsync(bool activeOnly, int page, int pageSize, CancellationToken ct = default) =>
            Task.FromResult(_accounts.Where(a => !activeOnly || a.User.IsActive).Skip((page - 1) * pageSize).Take(pageSize).ToList());

        public Task<int> CountAsync(bool activeOnly, CancellationToken ct = default) =>
            Task.FromResult(_accounts.Count(a => !activeOnly || a.User.IsActive));

        public Task UpdatePasswordAsync(Guid userId, PasswordHash hash, CancellationToken ct = default) => Task.CompletedTask;

        public Task AddAsync(User user, CancellationToken ct = default)
        {
            _accounts.Add(new UserAccount(user, ProfileFor(user.ProfileId)));
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken ct = default)
        {
            _accounts.RemoveAll(a => a.User.Id == user.Id);
            _accounts.Add(new UserAccount(user, ProfileFor(user.ProfileId)));
            return Task.CompletedTask;
        }

        public Task<Profile?> GetProfileByIdAsync(Guid profileId, CancellationToken ct = default) =>
            Task.FromResult<Profile?>(ProfileFor(profileId));

        public Task<List<Profile>> GetAllProfilesAsync(CancellationToken ct = default) =>
            Task.FromResult(new List<Profile> { Full, Operational, Common });

        private static Profile ProfileFor(Guid id)
        {
            if (id == Full.Id) return Full;
            if (id == Operational.Id) return Operational;
            if (id == Common.Id) return Common;
            return null!;
        }
    }
}
