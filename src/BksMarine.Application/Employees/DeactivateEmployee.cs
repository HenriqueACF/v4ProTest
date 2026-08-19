using BksMarine.Application.Common;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Employees;

public interface IDeactivateEmployee
{
    Task<Result> ExecuteAsync(Guid id, CancellationToken ct = default);
}

public sealed class DeactivateEmployee : IDeactivateEmployee
{
    private readonly IUserRepository _users;

    public DeactivateEmployee(IUserRepository users) => _users = users;

    public async Task<Result> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var account = await _users.GetByIdAsync(id, ct);
        if (account is null)
            return Result.Fail(new Error("employees.not_found", "Employee not found."));
        if (!account.User.IsActive)
            return Result.Fail(new Error("employees.already_inactive", "Employee is already inactive."));

        var user = account.User;
        var updated = new Core.Domain.Users.User(
            user.Id, user.Name, user.JobTitle, user.Email, user.PasswordHash, user.ProfileId, isActive: false);
        await _users.UpdateAsync(updated, ct);

        return Result.Ok();
    }
}
