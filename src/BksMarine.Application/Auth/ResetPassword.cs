using BksMarine.Application.Common;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Auth;

public sealed record ResetPasswordTransaction(string CurrentPassword, string NewPassword);

public interface IResetPassword
{
    Task<Result> ExecuteAsync(Guid userId, ResetPasswordTransaction txc, CancellationToken ct = default);
}

public sealed class ResetPassword : IResetPassword
{
    private const int MinPasswordLength = 8;

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;

    public ResetPassword(IUserRepository users, IPasswordHasher hasher)
    {
        _users = users;
        _hasher = hasher;
    }

    public async Task<Result> ExecuteAsync(Guid userId, ResetPasswordTransaction txc, CancellationToken ct = default)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(txc.NewPassword) || txc.NewPassword.Length < MinPasswordLength)
            return Result.Fail(new Error("validation.new_password", $"New password must be at least {MinPasswordLength} characters."));
        if (string.IsNullOrWhiteSpace(txc.CurrentPassword))
            return Result.Fail(new Error("validation.current_password", "Current password is required."));

        // Processing
        var account = await _users.GetByIdAsync(userId, ct);
        if (account is null || !account.User.IsActive || !_hasher.Verify(txc.CurrentPassword, account.User.PasswordHash))
            return Result.Fail(new Error("auth.invalid_credentials", "Invalid credentials."));

        var user = account.User;
        var updated = new Core.Domain.Users.User(
            user.Id, user.Name, user.JobTitle, user.Email, _hasher.Hash(txc.NewPassword), user.ProfileId, user.IsActive);
        await _users.UpdatePasswordAsync(user.Id, updated.PasswordHash, ct);

        return Result.Ok();
    }
}
