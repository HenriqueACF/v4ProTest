using BksMarine.Application.Common;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Operations;

public interface IListShips
{
    Task<Result<List<ShipResult>>> ExecuteAsync(bool activeOnly = true, CancellationToken ct = default);
}

public sealed class ListShips : IListShips
{
    private readonly IShipRepository _ships;

    public ListShips(IShipRepository ships) => _ships = ships;

    public async Task<Result<List<ShipResult>>> ExecuteAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        var ships = await _ships.ListAsync(activeOnly, ct);
        return Result<List<ShipResult>>.Ok(ships.Select(CreateShip.ToResult).ToList());
    }
}
