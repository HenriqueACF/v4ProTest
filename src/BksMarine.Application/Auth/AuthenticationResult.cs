using BksMarine.Core.Domain.Profiles;

namespace BksMarine.Application.Auth;

public sealed record AuthenticationResult(
    string Token,
    DateTime ExpiresAt,
    ProfileName Profile,
    IReadOnlyCollection<Module> Menu);
