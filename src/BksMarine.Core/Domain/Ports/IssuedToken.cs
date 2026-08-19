namespace BksMarine.Core.Domain.Ports;

public sealed record IssuedToken(string Token, DateTime ExpiresAt);
