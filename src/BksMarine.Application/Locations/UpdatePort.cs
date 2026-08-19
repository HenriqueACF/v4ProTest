using BksMarine.Application.Common;
using BksMarine.Core.Domain.Locations;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Locations;

public interface IUpdatePort
{
    Task<Result<PortResult>> ExecuteAsync(UpdatePortTransaction txc, CancellationToken ct = default);
}

public sealed class UpdatePort : IUpdatePort
{
    private readonly IPortRepository _ports;

    public UpdatePort(IPortRepository ports) => _ports = ports;

    public async Task<Result<PortResult>> ExecuteAsync(UpdatePortTransaction txc, CancellationToken ct = default)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(txc.Name))
            return Result<PortResult>.Fail(new Error("validation.name", "Port name is required."));
        if (!PortCode.IsValid(txc.Code))
            return Result<PortResult>.Fail(new Error("validation.code", "A valid port code is required."));

        var port = await _ports.GetByIdAsync(txc.Id, ct);
        if (port is null)
            return Result<PortResult>.Fail(new Error("locations.port.not_found", "Port not found."));

        var code = new PortCode(txc.Code);
        var existing = await _ports.GetByCodeAsync(code.Value, ct);
        if (existing is not null && existing.Id != txc.Id)
            return Result<PortResult>.Fail(new Error("locations.port.code_duplicate", "Port code already in use."));

        var updated = new Port(port.Id, txc.Name.Trim(), code, txc.Address, txc.Contact, txc.Notes, port.IsActive);
        await _ports.UpdateAsync(updated, ct);

        return Result<PortResult>.Ok(CreatePort.ToResult(updated));
    }
}
