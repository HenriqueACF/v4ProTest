using BksMarine.Application.Common;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Locations;

public interface IListPorts
{
    Task<Result<PageResult<PortResult>>> ExecuteAsync(bool activeOnly = true, int? page = null, int? pageSize = null, CancellationToken ct = default);
}

public sealed class ListPorts : IListPorts
{
    private readonly IPortRepository _ports;

    public ListPorts(IPortRepository ports) => _ports = ports;

    public async Task<Result<PageResult<PortResult>>> ExecuteAsync(bool activeOnly = true, int? page = null, int? pageSize = null, CancellationToken ct = default)
    {
        var (p, ps) = Paging.Normalize(page, pageSize);
        var total = await _ports.CountAsync(activeOnly, ct);
        var ports = await _ports.ListAsync(activeOnly, p, ps, ct);
        return Result<PageResult<PortResult>>.Ok(new PageResult<PortResult>(
            ports.Select(CreatePort.ToResult).ToList(), p, ps, total));
    }
}
