using BksMarine.Core.Domain.Ports;
using BksMarine.Core.Domain.Users;

namespace BksMarine.Application.Auth;

internal static class AuthResultFactory
{
    public static async Task<AuthenticationResult> BuildAsync(
        UserAccount account,
        DateTime now,
        AuthThrottleOptions throttle,
        IRefreshTokenRepository refreshTokens,
        ITokenService tokens,
        CancellationToken ct)
    {
        var access = tokens.Issue(account.User, account.Profile);
        var refreshRaw = Guid.NewGuid().ToString("N");
        var refreshExpiresAt = now.AddDays(throttle.RefreshLifetimeDays);
        var refreshToken = new RefreshToken(
            Guid.NewGuid(), account.User.Id, Hashing.Sha256(refreshRaw), refreshExpiresAt);
        await refreshTokens.SaveAsync(refreshToken, ct);

        var menu = account.Profile.AllowedModules.OrderBy(m => m).ToList();
        return new AuthenticationResult(
            access.Token, access.ExpiresAt, refreshRaw, refreshExpiresAt, account.Profile.Name, menu);
    }
}
