using BksMarine.Application.Common;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Employees;

public interface IListEmployees
{
    Task<Result<List<EmployeeResult>>> ExecuteAsync(bool activeOnly = true, CancellationToken ct = default);
}

public sealed class ListEmployees : IListEmployees
{
    private readonly IUserRepository _users;

    public ListEmployees(IUserRepository users) => _users = users;

    public async Task<Result<List<EmployeeResult>>> ExecuteAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        var accounts = await _users.ListAsync(activeOnly, ct);
        return Result<List<EmployeeResult>>.Ok(
            accounts.Select(a => CreateEmployee.ToResult(a.User, a.Profile.Name)).ToList());
    }
}
