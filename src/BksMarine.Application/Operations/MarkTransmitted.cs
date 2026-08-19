using BksMarine.Application.Common;
using BksMarine.Core.Domain.Operations;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Operations;

public interface IMarkTransmitted
{
    Task<Result> ExecuteAsync(Guid id, CancellationToken ct = default);
}

public sealed class MarkTransmitted : IMarkTransmitted
{
    private readonly IOperationRepository _operations;

    public MarkTransmitted(IOperationRepository operations) => _operations = operations;

    public async Task<Result> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var operation = await _operations.GetByIdAsync(id, ct);
        if (operation is null)
            return Result.Fail(new Error("operations.not_found", "Operation not found."));

        if (operation.TransmissionStatus == TransmissionStatus.Transmitted)
            return Result.Ok(); // idempotent

        var updated = Rebuild(operation, TransmissionStatus.Transmitted);
        await _operations.UpdateAsync(updated, ct);
        return Result.Ok();
    }

    internal static Operation Rebuild(Operation o, TransmissionStatus status) =>
        new(
            o.Id, o.Type, o.ShipId, o.PortId, o.BerthId,
            o.AgencyName, o.PilotName, o.PilotBoardingTime,
            o.TugBowName, o.TugBowTime, o.TugSternName, o.TugSternTime,
            o.FirstLineTime, o.LastLineTime,
            o.DraftBow, o.DraftMidship, o.DraftStern,
            o.Side, o.Notes, o.OccurredAt, o.UndockingTime,
            o.Photos, status, o.CreatedAt);
}
