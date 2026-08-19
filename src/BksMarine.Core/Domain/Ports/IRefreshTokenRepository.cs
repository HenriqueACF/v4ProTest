using BksMarine.Core.Domain.Users;

namespace BksMarine.Core.Domain.Ports;

public interface IRefreshTokenRepository
{
    Task SaveAsync(RefreshToken token, CancellationToken ct = default);
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
    Task RevokeAsync(Guid id, DateTime revokedAt, CancellationToken ct = default);
}
