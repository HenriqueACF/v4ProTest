using BksMarine.Application.Auth;
using BksMarine.Core.Domain.Ports;
using BksMarine.Core.Domain.Profiles;
using BksMarine.Core.Domain.Users;
using Xunit;

namespace BksMarine.Tests;

public sealed class AuthAdvTests
{
    private static readonly AuthThrottleOptions Throttle = new();

    [Fact]
    public async Task Login_throttles_after_max_failures()
    {
        var account = Account("a@b.com");
        var attempts = new FakeLoginAttempts(failures: Throttle.MaxFailures);
        var useCase = new AuthenticateUser(
            new FakeUserRepository(account), new FakePasswordHasher(), new FakeTokenService(),
            attempts, new FakeRefreshTokens(), Throttle);

        var result = await useCase.AuthenticateAsync(new AuthenticateTransaction("a@b.com", "secret"));

        Assert.True(result.IsFailure);
        Assert.Equal("auth.throttled", result.Error!.Code);
    }

    [Fact]
    public async Task Login_success_emits_refresh_token()
    {
        var account = Account("a@b.com");
        var refreshTokens = new FakeRefreshTokens();
        var useCase = new AuthenticateUser(
            new FakeUserRepository(account), new FakePasswordHasher(), new FakeTokenService(),
            new FakeLoginAttempts(), refreshTokens, Throttle);

        var result = await useCase.AuthenticateAsync(new AuthenticateTransaction("a@b.com", "secret"));

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value!.RefreshToken));
        Assert.Single(refreshTokens.Saved);
    }

    [Fact]
    public async Task Refresh_session_rotates_token()
    {
        var account = Account("a@b.com");
        var stored = new RefreshToken(Guid.NewGuid(), account.User.Id, Hashing.Sha256("old"), DateTime.UtcNow.AddDays(1));
        var refreshTokens = new FakeRefreshTokens(stored);
        var useCase = new RefreshSession(
            refreshTokens, new FakeUserRepository(account), new FakeTokenService(), Throttle);

        var result = await useCase.ExecuteAsync(new RefreshTransaction("old"));

        Assert.True(result.IsSuccess);
        Assert.Equal(Hashing.Sha256("old"), refreshTokens.Revoked);
        Assert.Single(refreshTokens.Saved); // apenas o novo token é salvo
    }

    [Fact]
    public async Task Refresh_session_invalid_token_fails()
    {
        var account = Account("a@b.com");
        var useCase = new RefreshSession(
            new FakeRefreshTokens(), new FakeUserRepository(account), new FakeTokenService(), Throttle);

        var result = await useCase.ExecuteAsync(new RefreshTransaction("unknown"));

        Assert.True(result.IsFailure);
        Assert.Equal("auth.invalid_refresh", result.Error!.Code);
    }

    [Fact]
    public async Task Logout_revokes_token()
    {
        var stored = new RefreshToken(Guid.NewGuid(), Guid.NewGuid(), Hashing.Sha256("tok"), DateTime.UtcNow.AddDays(1));
        var refreshTokens = new FakeRefreshTokens(stored);
        var useCase = new LogoutSession(refreshTokens);

        var result = await useCase.ExecuteAsync(new LogoutTransaction("tok"));

        Assert.True(result.IsSuccess);
        Assert.Equal(Hashing.Sha256("tok"), refreshTokens.Revoked);
    }

    private static UserAccount Account(string email)
    {
        var profile = new Profile(Guid.NewGuid(), ProfileName.Full, new[] { Module.Configuration, Module.Operations, Module.Reports });
        var user = new User(Guid.NewGuid(), "Ana", null, new Email(email), new PasswordHash("secret-h"), profile.Id, true);
        return new UserAccount(user, profile);
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public PasswordHash Hash(string plainPassword) => new(plainPassword + "-h");
        public bool Verify(string plainPassword, PasswordHash hash) => hash.Value == plainPassword + "-h";
    }

    private sealed class FakeTokenService : ITokenService
    {
        public IssuedToken Issue(User user, Profile profile) => new("fake-token", DateTime.UtcNow.AddHours(8));
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly UserAccount? _account;

        public FakeUserRepository(UserAccount? account = null) => _account = account;

        public Task<UserAccount?> GetByEmailAsync(Email email, CancellationToken ct = default) =>
            Task.FromResult(_account is not null && _account.User.Email.Value == email.Value ? _account : null);

        public Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_account is not null && _account.User.Id == id ? _account : null);

        public Task<List<UserAccount>> ListAsync(bool activeOnly, int page, int pageSize, CancellationToken ct = default) => Task.FromResult(new List<UserAccount>());
        public Task<int> CountAsync(bool activeOnly, CancellationToken ct = default) => Task.FromResult(0);
        public Task AddAsync(User user, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(User user, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdatePasswordAsync(Guid userId, PasswordHash hash, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Profile?> GetProfileByIdAsync(Guid profileId, CancellationToken ct = default) => Task.FromResult<Profile?>(_account?.Profile);
        public Task<List<Profile>> GetAllProfilesAsync(CancellationToken ct = default) => Task.FromResult(new List<Profile>());
    }

    private sealed class FakeLoginAttempts : ILoginAttemptRepository
    {
        private readonly int _failures;

        public FakeLoginAttempts(int failures = 0) => _failures = failures;

        public Task RegisterAsync(string email, bool success, DateTime attemptedAt, CancellationToken ct = default) => Task.CompletedTask;
        public Task<int> CountRecentFailuresAsync(string email, DateTime since, CancellationToken ct = default) => Task.FromResult(_failures);
        public Task ClearAsync(string email, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeRefreshTokens : IRefreshTokenRepository
    {
        private readonly RefreshToken? _stored;
        public List<RefreshToken> Saved { get; } = new();
        public string? Revoked { get; private set; }

        public FakeRefreshTokens(RefreshToken? stored = null) => _stored = stored;

        public Task SaveAsync(RefreshToken token, CancellationToken ct = default)
        {
            Saved.Add(token);
            return Task.CompletedTask;
        }

        public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
            Task.FromResult(_stored is not null && _stored.TokenHash == tokenHash ? _stored : null);

        public Task RevokeAsync(Guid id, DateTime revokedAt, CancellationToken ct = default)
        {
            if (_stored is not null && _stored.Id == id)
                Revoked = _stored.TokenHash;
            return Task.CompletedTask;
        }
    }
}
