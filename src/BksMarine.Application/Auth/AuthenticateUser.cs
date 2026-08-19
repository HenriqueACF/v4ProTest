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

    public AuthenticateUser(IUserRepository users, IPasswordHasher hasher, ITokenService tokens)
    {
        _users = users;
        _hasher = hasher;
        _tokens = tokens;
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

        // Pipeline stage 2 — Processing (generic error: no account enumeration)
        var account = await _users.GetByEmailAsync(new Email(txc.Email), ct);
        var valid = account is not null
            && account.User.IsActive
            && _hasher.Verify(txc.PlainPassword, account.User.PasswordHash);
        if (!valid)
            return Result<AuthenticationResult>.Fail(new Error("auth.invalid_credentials", "Invalid credentials."));

        // Pipeline stage 3 — Post-processing (issue JWT, build menu)
        var token = _tokens.Issue(account!.User, account.Profile);
        var menu = account.Profile.AllowedModules.OrderBy(m => m).ToList();
        var result = new AuthenticationResult(token.Token, token.ExpiresAt, account.Profile.Name, menu);
        return Result<AuthenticationResult>.Ok(result);
    }
}
