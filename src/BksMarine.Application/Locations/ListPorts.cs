using BksMarine.Application.Common;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Locations;

public interface IListPorts
{
    Task<Result<List<PortResult>>> ExecuteAsync(bool activeOnly = true, CancellationToken ct = default);
}

public sealed class ListPorts : IListPorts
{
    private readonly IPortRepository _ports;

    public ListPorts(IPortRepository ports) => _ports = ports;

    public async Task<Result<List<PortResult>>> ExecuteAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        var ports = await _ports.ListAsync(activeOnly, ct);
        return Result<List<PortResult>>.Ok(ports.Select(CreatePort.ToResult).ToList());
    }
}
