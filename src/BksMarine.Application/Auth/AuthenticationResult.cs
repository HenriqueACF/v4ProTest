using BksMarine.Core.Domain.Profiles;

namespace BksMarine.Application.Auth;

public sealed record AuthenticationResult(
    string Token,
    DateTime ExpiresAt,
    string RefreshToken,
    DateTime RefreshExpiresAt,
    ProfileName Profile,
    IReadOnlyCollection<Module> Menu);
