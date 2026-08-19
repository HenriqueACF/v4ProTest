using BksMarine.Application.Common;
using BksMarine.Core.Domain.Operations;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Operations;

public interface ICreateShip
{
    Task<Result<ShipResult>> ExecuteAsync(CreateShipTransaction txc, CancellationToken ct = default);
}

public sealed class CreateShip : ICreateShip
{
    private readonly IShipRepository _ships;

    public CreateShip(IShipRepository ships) => _ships = ships;

    public async Task<Result<ShipResult>> ExecuteAsync(CreateShipTransaction txc, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(txc.Name))
            return Result<ShipResult>.Fail(new Error("validation.name", "Ship name is required."));
        if (txc.Loa <= 0)
            return Result<ShipResult>.Fail(new Error("validation.loa", "LOA must be greater than zero."));
        if (txc.Dwt <= 0)
            return Result<ShipResult>.Fail(new Error("validation.dwt", "DWT must be greater than zero."));

        var ship = new Ship(Guid.NewGuid(), txc.Name.Trim(), txc.Loa, txc.Dwt, isActive: true);
        await _ships.AddAsync(ship, ct);

        return Result<ShipResult>.Ok(ToResult(ship));
    }

    internal static ShipResult ToResult(Ship ship) =>
        new(ship.Id, ship.Name, ship.Loa, ship.Dwt, ship.IsActive);
}
