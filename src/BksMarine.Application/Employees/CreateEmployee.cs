using BksMarine.Application.Common;
using BksMarine.Core.Domain.Ports;
using BksMarine.Core.Domain.Profiles;
using BksMarine.Core.Domain.Users;

namespace BksMarine.Application.Employees;

public interface ICreateEmployee
{
    Task<Result<EmployeeResult>> ExecuteAsync(CreateEmployeeTransaction txc, CancellationToken ct = default);
}

public sealed class CreateEmployee : ICreateEmployee
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;

    public CreateEmployee(IUserRepository users, IPasswordHasher hasher)
    {
        _users = users;
        _hasher = hasher;
    }

    public async Task<Result<EmployeeResult>> ExecuteAsync(CreateEmployeeTransaction txc, CancellationToken ct = default)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(txc.Name))
            return Result<EmployeeResult>.Fail(new Error("validation.name", "Employee name is required."));
        if (!Email.IsValid(txc.Email))
            return Result<EmployeeResult>.Fail(new Error("validation.email", "A valid email is required."));
        if (string.IsNullOrWhiteSpace(txc.Password))
            return Result<EmployeeResult>.Fail(new Error("validation.password", "A password is required."));
        if (txc.ProfileId == Guid.Empty)
            return Result<EmployeeResult>.Fail(new Error("validation.profile", "A profile is required."));

        // Processing
        if (await _users.GetByEmailAsync(new Email(txc.Email), ct) is not null)
            return Result<EmployeeResult>.Fail(new Error("employees.email_duplicate", "Email already in use."));
        if (await _users.GetProfileByIdAsync(txc.ProfileId, ct) is null)
            return Result<EmployeeResult>.Fail(new Error("employees.profile_not_found", "Profile not found."));

        var user = new User(
            Guid.NewGuid(),
            txc.Name.Trim(),
            txc.JobTitle,
            new Email(txc.Email),
            _hasher.Hash(txc.Password),
            txc.ProfileId,
            isActive: true);
        await _users.AddAsync(user, ct);

        var profile = (await _users.GetProfileByIdAsync(txc.ProfileId, ct))!;
        return Result<EmployeeResult>.Ok(ToResult(user, profile.Name));
    }

    internal static EmployeeResult ToResult(User user, ProfileName profile) =>
        new(user.Id, user.Name, user.Email.Value, user.JobTitle, profile, user.IsActive);
}
