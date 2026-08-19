namespace BksMarine.Core.Domain.Users;

public sealed class RefreshToken
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public string TokenHash { get; }
    public DateTime ExpiresAt { get; }
    public DateTime? RevokedAt { get; }

    public RefreshToken(Guid id, Guid userId, string tokenHash, DateTime expiresAt, DateTime? revokedAt = null)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        RevokedAt = revokedAt;
    }

    public bool IsActive(DateTime now) => RevokedAt is null && ExpiresAt > now;
}
