using BksMarine.Application.Common;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Locations;

public interface IDeactivatePort
{
    Task<Result> ExecuteAsync(Guid id, CancellationToken ct = default);
}

public sealed class DeactivatePort : IDeactivatePort
{
    private readonly IPortRepository _ports;

    public DeactivatePort(IPortRepository ports) => _ports = ports;

    public async Task<Result> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var port = await _ports.GetByIdAsync(id, ct);
        if (port is null)
            return Result.Fail(new Error("locations.port.not_found", "Port not found."));
        if (!port.IsActive)
            return Result.Fail(new Error("locations.port.already_inactive", "Port is already inactive."));

        var updated = new Core.Domain.Locations.Port(
            port.Id, port.Name, port.Code, port.Address, port.Contact, port.Notes, isActive: false);
        await _ports.UpdateAsync(updated, ct);

        return Result.Ok();
    }
}
