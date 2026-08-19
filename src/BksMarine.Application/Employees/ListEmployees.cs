using BksMarine.Application.Common;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Employees;

public interface IListEmployees
{
    Task<Result<PageResult<EmployeeResult>>> ExecuteAsync(bool activeOnly = true, int? page = null, int? pageSize = null, CancellationToken ct = default);
}

public sealed class ListEmployees : IListEmployees
{
    private readonly IUserRepository _users;

    public ListEmployees(IUserRepository users) => _users = users;

    public async Task<Result<PageResult<EmployeeResult>>> ExecuteAsync(bool activeOnly = true, int? page = null, int? pageSize = null, CancellationToken ct = default)
    {
        var (p, ps) = Paging.Normalize(page, pageSize);
        var total = await _users.CountAsync(activeOnly, ct);
        var accounts = await _users.ListAsync(activeOnly, p, ps, ct);
        return Result<PageResult<EmployeeResult>>.Ok(new PageResult<EmployeeResult>(
            accounts.Select(a => CreateEmployee.ToResult(a.User, a.Profile.Name)).ToList(), p, ps, total));
    }
}
