using BksMarine.Application.Common;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Operations;

public interface IGetOperationDetail
{
    Task<Result<OperationDetailResult>> ExecuteAsync(Guid id, CancellationToken ct = default);
}

public sealed class GetOperationDetail : IGetOperationDetail
{
    private readonly IOperationRepository _operations;
    private readonly IShipRepository _ships;
    private readonly IPortRepository _ports;
    private readonly IBerthRepository _berths;
    private readonly IUserRepository _users;

    public GetOperationDetail(
        IOperationRepository operations,
        IShipRepository ships,
        IPortRepository ports,
        IBerthRepository berths,
        IUserRepository users)
    {
        _operations = operations;
        _ships = ships;
        _ports = ports;
        _berths = berths;
        _users = users;
    }

    public async Task<Result<OperationDetailResult>> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var operation = await _operations.GetByIdAsync(id, ct);
        if (operation is null)
            return Result<OperationDetailResult>.Fail(new Error("operations.not_found", "Operation not found."));

        var ship = await _ships.GetByIdAsync(operation.ShipId, ct);
        var port = await _ports.GetByIdAsync(operation.PortId, ct);
        var berth = await _berths.GetByIdAsync(operation.BerthId, ct);
        var responsible = operation.ResponsibleUserId is not null
            ? await _users.GetByIdAsync(operation.ResponsibleUserId.Value, ct)
            : null;

        return Result<OperationDetailResult>.Ok(new OperationDetailResult(
            RegisterOperation.ToResult(operation),
            ship?.Name ?? "—",
            port?.Name ?? "—",
            berth?.Name ?? "—",
            responsible?.User.Name));
    }
}
