using BksMarine.Application.Common;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Auth;

public sealed record LogoutTransaction(string RefreshToken);

public interface ILogoutSession
{
    Task<Result> ExecuteAsync(LogoutTransaction txc, CancellationToken ct = default);
}

public sealed class LogoutSession : ILogoutSession
{
    private readonly IRefreshTokenRepository _refreshTokens;

    public LogoutSession(IRefreshTokenRepository refreshTokens) => _refreshTokens = refreshTokens;

    public async Task<Result> ExecuteAsync(LogoutTransaction txc, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(txc.RefreshToken))
            return Result.Ok();

        var stored = await _refreshTokens.GetByHashAsync(Hashing.Sha256(txc.RefreshToken), ct);
        if (stored is not null)
            await _refreshTokens.RevokeAsync(stored.Id, DateTime.UtcNow, ct);

        return Result.Ok();
    }
}
