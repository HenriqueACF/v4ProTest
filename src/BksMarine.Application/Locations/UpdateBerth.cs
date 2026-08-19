using BksMarine.Application.Common;
using BksMarine.Core.Domain.Locations;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Locations;

public interface IUpdateBerth
{
    Task<Result<BerthResult>> ExecuteAsync(UpdateBerthTransaction txc, CancellationToken ct = default);
}

public sealed class UpdateBerth : IUpdateBerth
{
    private readonly IBerthRepository _berths;

    public UpdateBerth(IBerthRepository berths) => _berths = berths;

    public async Task<Result<BerthResult>> ExecuteAsync(UpdateBerthTransaction txc, CancellationToken ct = default)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(txc.Name))
            return Result<BerthResult>.Fail(new Error("validation.name", "Berth name is required."));
        if (txc.MaxLoa is <= 0)
            return Result<BerthResult>.Fail(new Error("validation.max_loa", "Max LOA must be greater than zero."));
        if (txc.MaxDwt is <= 0)
            return Result<BerthResult>.Fail(new Error("validation.max_dwt", "Max DWT must be greater than zero."));

        var berth = await _berths.GetByIdAsync(txc.Id, ct);
        if (berth is null)
            return Result<BerthResult>.Fail(new Error("locations.berth.not_found", "Berth not found."));

        var existing = await _berths.GetByNameInPortAsync(txc.Name.Trim(), berth.PortId, ct);
        if (existing is not null && existing.Id != txc.Id)
            return Result<BerthResult>.Fail(new Error("locations.berth.name_duplicate", "Berth name already in use in this port."));

        var updated = new Berth(
            berth.Id, txc.Name.Trim(), berth.PortId, txc.MaxLoa, txc.MaxDwt, txc.Type, txc.Notes, berth.IsActive);
        await _berths.UpdateAsync(updated, ct);

        return Result<BerthResult>.Ok(CreateBerth.ToResult(updated));
    }
}
