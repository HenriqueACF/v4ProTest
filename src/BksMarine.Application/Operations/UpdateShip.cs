using BksMarine.Application.Common;
using BksMarine.Core.Domain.Operations;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Operations;

public interface IUpdateShip
{
    Task<Result<ShipResult>> ExecuteAsync(UpdateShipTransaction txc, CancellationToken ct = default);
}

public sealed class UpdateShip : IUpdateShip
{
    private readonly IShipRepository _ships;

    public UpdateShip(IShipRepository ships) => _ships = ships;

    public async Task<Result<ShipResult>> ExecuteAsync(UpdateShipTransaction txc, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(txc.Name))
            return Result<ShipResult>.Fail(new Error("validation.name", "Ship name is required."));
        if (txc.Loa <= 0)
            return Result<ShipResult>.Fail(new Error("validation.loa", "LOA must be greater than zero."));
        if (txc.Dwt <= 0)
            return Result<ShipResult>.Fail(new Error("validation.dwt", "DWT must be greater than zero."));

        var ship = await _ships.GetByIdAsync(txc.Id, ct);
        if (ship is null)
            return Result<ShipResult>.Fail(new Error("operations.ship_not_found", "Ship not found."));

        var updated = new Ship(ship.Id, txc.Name.Trim(), txc.Loa, txc.Dwt, ship.IsActive);
        await _ships.UpdateAsync(updated, ct);

        return Result<ShipResult>.Ok(CreateShip.ToResult(updated));
    }
}
