using BksMarine.Application.Common;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Locations;

public interface IDeactivateBerth
{
    Task<Result> ExecuteAsync(Guid id, CancellationToken ct = default);
}

public sealed class DeactivateBerth : IDeactivateBerth
{
    private readonly IBerthRepository _berths;

    public DeactivateBerth(IBerthRepository berths) => _berths = berths;

    public async Task<Result> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var berth = await _berths.GetByIdAsync(id, ct);
        if (berth is null)
            return Result.Fail(new Error("locations.berth.not_found", "Berth not found."));
        if (!berth.IsActive)
            return Result.Fail(new Error("locations.berth.already_inactive", "Berth is already inactive."));

        var updated = new Core.Domain.Locations.Berth(
            berth.Id, berth.Name, berth.PortId, berth.MaxLoa, berth.MaxDwt, berth.Type, berth.Notes, isActive: false);
        await _berths.UpdateAsync(updated, ct);

        return Result.Ok();
    }
}
