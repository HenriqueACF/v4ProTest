using BksMarine.Application.Common;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Operations;

public interface IDeactivateShip
{
    Task<Result> ExecuteAsync(Guid id, CancellationToken ct = default);
}

public sealed class DeactivateShip : IDeactivateShip
{
    private readonly IShipRepository _ships;

    public DeactivateShip(IShipRepository ships) => _ships = ships;

    public async Task<Result> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var ship = await _ships.GetByIdAsync(id, ct);
        if (ship is null)
            return Result.Fail(new Error("operations.ship_not_found", "Ship not found."));
        if (!ship.IsActive)
            return Result.Fail(new Error("operations.ship_already_inactive", "Ship is already inactive."));

        var updated = new Core.Domain.Operations.Ship(ship.Id, ship.Name, ship.Loa, ship.Dwt, isActive: false);
        await _ships.UpdateAsync(updated, ct);

        return Result.Ok();
    }
}
