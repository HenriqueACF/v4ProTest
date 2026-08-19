using BksMarine.Application.Common;
using BksMarine.Core.Domain.Locations;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Locations;

public interface ICreateBerth
{
    Task<Result<BerthResult>> ExecuteAsync(CreateBerthTransaction txc, CancellationToken ct = default);
}

public sealed class CreateBerth : ICreateBerth
{
    private readonly IBerthRepository _berths;
    private readonly IPortRepository _ports;

    public CreateBerth(IBerthRepository berths, IPortRepository ports)
    {
        _berths = berths;
        _ports = ports;
    }

    public async Task<Result<BerthResult>> ExecuteAsync(CreateBerthTransaction txc, CancellationToken ct = default)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(txc.Name))
            return Result<BerthResult>.Fail(new Error("validation.name", "Berth name is required."));
        if (txc.MaxLoa is <= 0)
            return Result<BerthResult>.Fail(new Error("validation.max_loa", "Max LOA must be greater than zero."));
        if (txc.MaxDwt is <= 0)
            return Result<BerthResult>.Fail(new Error("validation.max_dwt", "Max DWT must be greater than zero."));

        // Processing
        var port = await _ports.GetByIdAsync(txc.PortId, ct);
        if (port is null)
            return Result<BerthResult>.Fail(new Error("locations.port.not_found", "Port not found."));
        if (!port.IsActive)
            return Result<BerthResult>.Fail(new Error("locations.port.inactive", "Port is inactive."));

        if (await _berths.GetByNameInPortAsync(txc.Name.Trim(), txc.PortId, ct) is not null)
            return Result<BerthResult>.Fail(new Error("locations.berth.name_duplicate", "Berth name already in use in this port."));

        var berth = new Berth(
            Guid.NewGuid(), txc.Name.Trim(), txc.PortId, txc.MaxLoa, txc.MaxDwt, txc.Type, txc.Notes, isActive: true);
        await _berths.AddAsync(berth, ct);

        return Result<BerthResult>.Ok(ToResult(berth));
    }

    internal static BerthResult ToResult(Berth berth) =>
        new(berth.Id, berth.Name, berth.PortId, berth.MaxLoa, berth.MaxDwt, berth.Type, berth.Notes, berth.IsActive);
}
