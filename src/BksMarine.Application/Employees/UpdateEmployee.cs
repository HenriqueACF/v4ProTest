using BksMarine.Application.Common;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Employees;

public interface IUpdateEmployee
{
    Task<Result<EmployeeResult>> ExecuteAsync(UpdateEmployeeTransaction txc, CancellationToken ct = default);
}

public sealed class UpdateEmployee : IUpdateEmployee
{
    private readonly IUserRepository _users;

    public UpdateEmployee(IUserRepository users) => _users = users;

    public async Task<Result<EmployeeResult>> ExecuteAsync(UpdateEmployeeTransaction txc, CancellationToken ct = default)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(txc.Name))
            return Result<EmployeeResult>.Fail(new Error("validation.name", "Employee name is required."));
        if (txc.ProfileId == Guid.Empty)
            return Result<EmployeeResult>.Fail(new Error("validation.profile", "A profile is required."));

        var account = await _users.GetByIdAsync(txc.Id, ct);
        if (account is null)
            return Result<EmployeeResult>.Fail(new Error("employees.not_found", "Employee not found."));
        var profile = await _users.GetProfileByIdAsync(txc.ProfileId, ct);
        if (profile is null)
            return Result<EmployeeResult>.Fail(new Error("employees.profile_not_found", "Profile not found."));

        var user = account.User;
        var updated = new Core.Domain.Users.User(
            user.Id, txc.Name.Trim(), txc.JobTitle, user.Email, user.PasswordHash, txc.ProfileId, user.IsActive);
        await _users.UpdateAsync(updated, ct);

        return Result<EmployeeResult>.Ok(CreateEmployee.ToResult(updated, profile.Name));
    }
}
