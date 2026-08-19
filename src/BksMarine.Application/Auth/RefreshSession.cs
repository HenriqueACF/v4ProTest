using BksMarine.Application.Common;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Auth;

public sealed record RefreshTransaction(string RefreshToken);

public interface IRefreshSession
{
    Task<Result<AuthenticationResult>> ExecuteAsync(RefreshTransaction txc, CancellationToken ct = default);
}

public sealed class RefreshSession : IRefreshSession
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUserRepository _users;
    private readonly ITokenService _tokens;
    private readonly AuthThrottleOptions _throttle;

    public RefreshSession(
        IRefreshTokenRepository refreshTokens,
        IUserRepository users,
        ITokenService tokens,
        AuthThrottleOptions throttle)
    {
        _refreshTokens = refreshTokens;
        _users = users;
        _tokens = tokens;
        _throttle = throttle;
    }

    public async Task<Result<AuthenticationResult>> ExecuteAsync(RefreshTransaction txc, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(txc.RefreshToken))
            return Result<AuthenticationResult>.Fail(new Error("validation.refresh", "A refresh token is required."));

        var now = DateTime.UtcNow;
        var stored = await _refreshTokens.GetByHashAsync(Hashing.Sha256(txc.RefreshToken), ct);
        if (stored is null || !stored.IsActive(now))
            return Result<AuthenticationResult>.Fail(new Error("auth.invalid_refresh", "Invalid or expired refresh token."));

        var account = await _users.GetByIdAsync(stored.UserId, ct);
        if (account is null || !account.User.IsActive)
            return Result<AuthenticationResult>.Fail(new Error("auth.invalid_refresh", "Invalid or expired refresh token."));

        // Rotate: revoga o token usado e emite novo par.
        await _refreshTokens.RevokeAsync(stored.Id, now, ct);
        return Result<AuthenticationResult>.Ok(
            await AuthResultFactory.BuildAsync(account, now, _throttle, _refreshTokens, _tokens, ct));
    }
}
