using BksMarine.Application.Auth;
using BksMarine.Core.Domain.Ports;
using BksMarine.Core.Domain.Profiles;
using BksMarine.Core.Domain.Users;
using Xunit;

namespace BksMarine.Tests;

public sealed class AuthenticateUserTests
{
    [Fact]
    public async Task Valid_active_user_returns_token_and_menu()
    {
        var account = Account("admin@bksmarine.com", ProfileName.Full, "secret");
        var useCase = new AuthenticateUser(
            new FakeUserRepository(account),
            new FakePasswordHasher(),
            new FakeTokenService(),
            new FakeLoginAttempts(),
            new FakeRefreshTokens(),
            new AuthThrottleOptions());

        var result = await useCase.AuthenticateAsync(
            new AuthenticateTransaction("admin@bksmarine.com", "secret"));

        Assert.True(result.IsSuccess);
        Assert.Equal("fake-token", result.Value!.Token);
        Assert.Equal(ProfileName.Full, result.Value.Profile);
        Assert.Equal(new[] { Module.Configuration, Module.Operations, Module.Reports }, result.Value.Menu);
    }

    [Theory]
    [InlineData(ProfileName.Full, Module.Configuration, Module.Operations, Module.Reports)]
    [InlineData(ProfileName.Operational, Module.Operations, Module.Reports)]
    [InlineData(ProfileName.Common, Module.Reports)]
    public async Task Menu_reflects_profile(ProfileName name, params Module[] expected)
    {
        var account = Account("user@bksmarine.com", name, "secret");
        var useCase = new AuthenticateUser(
            new FakeUserRepository(account),
            new FakePasswordHasher(),
            new FakeTokenService(),
            new FakeLoginAttempts(),
            new FakeRefreshTokens(),
            new AuthThrottleOptions());

        var result = await useCase.AuthenticateAsync(
            new AuthenticateTransaction("user@bksmarine.com", "secret"));

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value!.Menu);
    }

    [Fact]
    public async Task Wrong_password_returns_generic_invalid_credentials()
    {
        var account = Account("admin@bksmarine.com", ProfileName.Full, "secret");
        var useCase = new AuthenticateUser(
            new FakeUserRepository(account),
            new FakePasswordHasher(),
            new FakeTokenService(),
            new FakeLoginAttempts(),
            new FakeRefreshTokens(),
            new AuthThrottleOptions());

        var result = await useCase.AuthenticateAsync(
            new AuthenticateTransaction("admin@bksmarine.com", "wrong"));

        Assert.True(result.IsFailure);
        Assert.Equal("auth.invalid_credentials", result.Error!.Code);
    }

    [Fact]
    public async Task Unknown_email_returns_generic_invalid_credentials()
    {
        var account = Account("admin@bksmarine.com", ProfileName.Full, "secret");
        var useCase = new AuthenticateUser(
            new FakeUserRepository(account),
            new FakePasswordHasher(),
            new FakeTokenService(),
            new FakeLoginAttempts(),
            new FakeRefreshTokens(),
            new AuthThrottleOptions());

        var result = await useCase.AuthenticateAsync(
            new AuthenticateTransaction("nobody@bksmarine.com", "secret"));

        Assert.True(result.IsFailure);
        Assert.Equal("auth.invalid_credentials", result.Error!.Code);
    }

    [Fact]
    public async Task Inactive_user_does_not_authenticate()
    {
        var account = Account("admin@bksmarine.com", ProfileName.Full, "secret", active: false);
        var useCase = new AuthenticateUser(
            new FakeUserRepository(account),
            new FakePasswordHasher(),
            new FakeTokenService(),
            new FakeLoginAttempts(),
            new FakeRefreshTokens(),
            new AuthThrottleOptions());

        var result = await useCase.AuthenticateAsync(
            new AuthenticateTransaction("admin@bksmarine.com", "secret"));

        Assert.True(result.IsFailure);
        Assert.Equal("auth.invalid_credentials", result.Error!.Code);
    }

    [Fact]
    public async Task Invalid_email_returns_validation_error()
    {
        var useCase = new AuthenticateUser(
            new FakeUserRepository(),
            new FakePasswordHasher(),
            new FakeTokenService(),
            new FakeLoginAttempts(),
            new FakeRefreshTokens(),
            new AuthThrottleOptions());

        var result = await useCase.AuthenticateAsync(
            new AuthenticateTransaction("not-an-email", "secret"));

        Assert.True(result.IsFailure);
        Assert.Equal("validation.email", result.Error!.Code);
    }

    [Fact]
    public async Task Missing_password_returns_validation_error()
    {
        var useCase = new AuthenticateUser(
            new FakeUserRepository(),
            new FakePasswordHasher(),
            new FakeTokenService(),
            new FakeLoginAttempts(),
            new FakeRefreshTokens(),
            new AuthThrottleOptions());

        var result = await useCase.AuthenticateAsync(
            new AuthenticateTransaction("admin@bksmarine.com", ""));

        Assert.True(result.IsFailure);
        Assert.Equal("validation.password", result.Error!.Code);
    }

    private static UserAccount Account(string email, ProfileName name, string password, bool active = true)
    {
        var profile = new Profile(Guid.NewGuid(), name, ModulesFor(name));
        var user = new User(Guid.NewGuid(), "Test User", null, new Email(email), new PasswordHash(password), profile.Id, active);
        return new UserAccount(user, profile);
    }

    private static IReadOnlyCollection<Module> ModulesFor(ProfileName name) => name switch
    {
        ProfileName.Full => new[] { Module.Configuration, Module.Operations, Module.Reports },
        ProfileName.Operational => new[] { Module.Operations, Module.Reports },
        _ => new[] { Module.Reports }
    };

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly UserAccount? _account;

        public FakeUserRepository(UserAccount? account = null) => _account = account;

        public Task<UserAccount?> GetByEmailAsync(Email email, CancellationToken ct = default) =>
            Task.FromResult(_account is not null && _account.User.Email.Value == email.Value ? _account : null);

        public Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_account is not null && _account.User.Id == id ? _account : null);

        public Task<List<UserAccount>> ListAsync(bool activeOnly, int page, int pageSize, CancellationToken ct = default) =>
            Task.FromResult(_account is null ? new List<UserAccount>() : new List<UserAccount> { _account });

        public Task<int> CountAsync(bool activeOnly, CancellationToken ct = default) =>
            Task.FromResult(_account is null ? 0 : 1);

        public Task AddAsync(User user, CancellationToken ct = default) => Task.CompletedTask;

        public Task UpdateAsync(User user, CancellationToken ct = default) => Task.CompletedTask;

        public Task UpdatePasswordAsync(Guid userId, PasswordHash hash, CancellationToken ct = default) => Task.CompletedTask;

        public Task<Profile?> GetProfileByIdAsync(Guid profileId, CancellationToken ct = default) =>
            Task.FromResult(_account is not null && _account.Profile.Id == profileId ? _account.Profile : null);

        public Task<List<Profile>> GetAllProfilesAsync(CancellationToken ct = default) =>
            Task.FromResult(_account is null ? new List<Profile>() : new List<Profile> { _account.Profile });
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public PasswordHash Hash(string plainPassword) => new(plainPassword);
        public bool Verify(string plainPassword, PasswordHash hash) => plainPassword == hash.Value;
    }

    private sealed class FakeTokenService : ITokenService
    {
        public IssuedToken Issue(User user, Profile profile) =>
            new("fake-token", DateTime.UtcNow.AddHours(8));
    }

    private sealed class FakeLoginAttempts : ILoginAttemptRepository
    {
        public Task RegisterAsync(string email, bool success, DateTime attemptedAt, CancellationToken ct = default) => Task.CompletedTask;
        public Task<int> CountRecentFailuresAsync(string email, DateTime since, CancellationToken ct = default) => Task.FromResult(0);
        public Task ClearAsync(string email, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeRefreshTokens : IRefreshTokenRepository
    {
        public Task SaveAsync(RefreshToken token, CancellationToken ct = default) => Task.CompletedTask;
        public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default) => Task.FromResult<RefreshToken?>(null);
        public Task RevokeAsync(Guid id, DateTime revokedAt, CancellationToken ct = default) => Task.CompletedTask;
    }
}
