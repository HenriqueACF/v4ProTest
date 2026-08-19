namespace BksMarine.Application.Operations;

public sealed record CreateShipTransaction(string Name, decimal Loa, decimal Dwt);

public sealed record UpdateShipTransaction(Guid Id, string Name, decimal Loa, decimal Dwt);

public sealed record ShipResult(Guid Id, string Name, decimal Loa, decimal Dwt, bool IsActive);
