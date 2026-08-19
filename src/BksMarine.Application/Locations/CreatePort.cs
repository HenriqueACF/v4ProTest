using BksMarine.Application.Common;
using BksMarine.Core.Domain.Locations;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Locations;

public interface ICreatePort
{
    Task<Result<PortResult>> ExecuteAsync(CreatePortTransaction txc, CancellationToken ct = default);
}

public sealed class CreatePort : ICreatePort
{
    private readonly IPortRepository _ports;

    public CreatePort(IPortRepository ports) => _ports = ports;

    public async Task<Result<PortResult>> ExecuteAsync(CreatePortTransaction txc, CancellationToken ct = default)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(txc.Name))
            return Result<PortResult>.Fail(new Error("validation.name", "Port name is required."));
        if (!PortCode.IsValid(txc.Code))
            return Result<PortResult>.Fail(new Error("validation.code", "A valid port code is required (letters/numbers)."));

        var code = new PortCode(txc.Code);

        // Processing
        if (await _ports.GetByCodeAsync(code.Value, ct) is not null)
            return Result<PortResult>.Fail(new Error("locations.port.code_duplicate", "Port code already in use."));

        var port = new Port(Guid.NewGuid(), txc.Name.Trim(), code, txc.Address, txc.Contact, txc.Notes, isActive: true);
        await _ports.AddAsync(port, ct);

        return Result<PortResult>.Ok(ToResult(port));
    }

    internal static PortResult ToResult(Port port) =>
        new(port.Id, port.Name, port.Code.Value, port.Address, port.Contact, port.Notes, port.IsActive);
}
