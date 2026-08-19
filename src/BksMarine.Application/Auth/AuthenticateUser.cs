using BksMarine.Application.Common;
using BksMarine.Core.Domain.Ports;
using BksMarine.Core.Domain.Users;

namespace BksMarine.Application.Auth;

public interface IAuthenticateUser
{
    Task<Result<AuthenticationResult>> AuthenticateAsync(AuthenticateTransaction txc, CancellationToken ct = default);
}

public sealed class AuthenticateUser : IAuthenticateUser
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly ILoginAttemptRepository _loginAttempts;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly AuthThrottleOptions _throttle;

    public AuthenticateUser(
        IUserRepository users,
        IPasswordHasher hasher,
        ITokenService tokens,
        ILoginAttemptRepository loginAttempts,
        IRefreshTokenRepository refreshTokens,
        AuthThrottleOptions throttle)
    {
        _users = users;
        _hasher = hasher;
        _tokens = tokens;
        _loginAttempts = loginAttempts;
        _refreshTokens = refreshTokens;
        _throttle = throttle;
    }

    public async Task<Result<AuthenticationResult>> AuthenticateAsync(
        AuthenticateTransaction txc,
        CancellationToken ct = default)
    {
        // Pipeline stage 1 — Validation
        if (!Email.IsValid(txc.Email))
            return Result<AuthenticationResult>.Fail(new Error("validation.email", "A valid email is required."));
        if (string.IsNullOrWhiteSpace(txc.PlainPassword))
            return Result<AuthenticationResult>.Fail(new Error("validation.password", "A password is required."));

        var now = DateTime.UtcNow;

        // Stage 2 — Throttle (bloqueio por tentativas)
        var failures = await _loginAttempts.CountRecentFailuresAsync(
            txc.Email, now.AddMinutes(-_throttle.WindowMinutes), ct);
        if (failures >= _throttle.MaxFailures)
            return Result<AuthenticationResult>.Fail(new Error("auth.throttled", "Too many failed attempts. Try again later."));

        // Stage 3 — Processing (credenciais; erro genérico: anti-enumeração)
        var account = await _users.GetByEmailAsync(new Email(txc.Email), ct);
        var valid = account is not null
            && account.User.IsActive
            && _hasher.Verify(txc.PlainPassword, account.User.PasswordHash);

        await _loginAttempts.RegisterAsync(txc.Email, valid, now, ct);
        if (!valid)
            return Result<AuthenticationResult>.Fail(new Error("auth.invalid_credentials", "Invalid credentials."));
        await _loginAttempts.ClearAsync(txc.Email, ct);

        // Stage 4 — Post-processing (access + refresh)
        return Result<AuthenticationResult>.Ok(
            await AuthResultFactory.BuildAsync(account!, now, _throttle, _refreshTokens, _tokens, ct));
    }
}
