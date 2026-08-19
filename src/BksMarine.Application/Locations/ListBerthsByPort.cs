using BksMarine.Application.Common;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Locations;

public interface IListBerthsByPort
{
    Task<Result<PageResult<BerthResult>>> ExecuteAsync(Guid portId, bool activeOnly = true, int? page = null, int? pageSize = null, CancellationToken ct = default);
}

public sealed class ListBerthsByPort : IListBerthsByPort
{
    private readonly IBerthRepository _berths;

    public ListBerthsByPort(IBerthRepository berths) => _berths = berths;

    public async Task<Result<PageResult<BerthResult>>> ExecuteAsync(Guid portId, bool activeOnly = true, int? page = null, int? pageSize = null, CancellationToken ct = default)
    {
        var (p, ps) = Paging.Normalize(page, pageSize);
        var total = await _berths.CountByPortAsync(portId, activeOnly, ct);
        var berths = await _berths.ListByPortAsync(portId, activeOnly, p, ps, ct);
        return Result<PageResult<BerthResult>>.Ok(new PageResult<BerthResult>(
            berths.Select(CreateBerth.ToResult).ToList(), p, ps, total));
    }
}
