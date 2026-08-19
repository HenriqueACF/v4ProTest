using BksMarine.Application.Common;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Locations;

public interface IListBerthsByPort
{
    Task<Result<List<BerthResult>>> ExecuteAsync(Guid portId, bool activeOnly = true, CancellationToken ct = default);
}

public sealed class ListBerthsByPort : IListBerthsByPort
{
    private readonly IBerthRepository _berths;

    public ListBerthsByPort(IBerthRepository berths) => _berths = berths;

    public async Task<Result<List<BerthResult>>> ExecuteAsync(Guid portId, bool activeOnly = true, CancellationToken ct = default)
    {
        var berths = await _berths.ListByPortAsync(portId, activeOnly, ct);
        return Result<List<BerthResult>>.Ok(berths.Select(CreateBerth.ToResult).ToList());
    }
}
