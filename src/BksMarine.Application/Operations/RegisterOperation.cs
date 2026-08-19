using BksMarine.Application.Common;
using BksMarine.Core.Domain.Locations;
using BksMarine.Core.Domain.Operations;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Operations;

public interface IRegisterOperation
{
    Task<Result<OperationResult>> ExecuteAsync(RegisterOperationTransaction txc, CancellationToken ct = default);
}

public sealed class RegisterOperation : IRegisterOperation
{
    private const int MaxPhotos = 6;

    private readonly IOperationRepository _operations;
    private readonly IShipRepository _ships;
    private readonly IPortRepository _ports;
    private readonly IBerthRepository _berths;
    private readonly IStorageClient _storage;

    public RegisterOperation(
        IOperationRepository operations,
        IShipRepository ships,
        IPortRepository ports,
        IBerthRepository berths,
        IStorageClient storage)
    {
        _operations = operations;
        _ships = ships;
        _ports = ports;
        _berths = berths;
        _storage = storage;
    }

    public async Task<Result<OperationResult>> ExecuteAsync(RegisterOperationTransaction txc, CancellationToken ct = default)
    {
        // Stage 1 — Validation
        if (txc.Photos.Count > MaxPhotos)
            return Result<OperationResult>.Fail(new Error("operations.too_many_photos", $"At most {MaxPhotos} photos are allowed."));
        if (txc.DraftBow is < 0)
            return Result<OperationResult>.Fail(new Error("validation.draft_bow", "Draft bow must be non-negative."));
        if (txc.DraftMidship is < 0)
            return Result<OperationResult>.Fail(new Error("validation.draft_midship", "Draft midship must be non-negative."));
        if (txc.DraftStern is < 0)
            return Result<OperationResult>.Fail(new Error("validation.draft_stern", "Draft stern must be non-negative."));
        if (txc.FirstLineTime is not null && txc.LastLineTime is not null && txc.FirstLineTime >= txc.LastLineTime)
            return Result<OperationResult>.Fail(new Error("operations.invalid_line_times", "First line time must be before last line time."));
        if (txc.Type == OperationType.Undocking && txc.UndockingTime is null)
            return Result<OperationResult>.Fail(new Error("validation.undocking_time", "Undocking time is required for undocking operations."));

        // Stage 2 — Enrichment (load references, validate active + ownership)
        var ship = await _ships.GetByIdAsync(txc.ShipId, ct);
        if (ship is null)
            return Result<OperationResult>.Fail(new Error("operations.ship_not_found", "Ship not found."));
        if (!ship.IsActive)
            return Result<OperationResult>.Fail(new Error("operations.ship_inactive", "Ship is inactive."));

        var port = await _ports.GetByIdAsync(txc.PortId, ct);
        if (port is null)
            return Result<OperationResult>.Fail(new Error("operations.port_not_found", "Port not found."));
        if (!port.IsActive)
            return Result<OperationResult>.Fail(new Error("operations.port_inactive", "Port is inactive."));

        var berth = await _berths.GetByIdAsync(txc.BerthId, ct);
        if (berth is null)
            return Result<OperationResult>.Fail(new Error("operations.berth_not_found", "Berth not found."));
        if (!berth.IsActive)
            return Result<OperationResult>.Fail(new Error("operations.berth_inactive", "Berth is inactive."));
        if (berth.PortId != txc.PortId)
            return Result<OperationResult>.Fail(new Error("operations.berth_not_in_port", "Berth does not belong to the informed port."));

        // Stage 3 — Processing (save photos, persist)
        var photoUrls = new List<string>();
        foreach (var photo in txc.Photos)
        {
            string url;
            try
            {
                url = await _storage.SaveAsync(photo, ct);
            }
            catch (Exception ex)
            {
                return Result<OperationResult>.Fail(
                    new Error("operations.photo_upload_failed", $"Photo upload failed: {ex.Message}"));
            }
            photoUrls.Add(url);
        }

        var operation = new Operation(
            Guid.NewGuid(),
            txc.Type,
            txc.ShipId,
            txc.PortId,
            txc.BerthId,
            txc.AgencyName,
            txc.PilotName,
            txc.PilotBoardingTime,
            txc.TugBowName,
            txc.TugBowTime,
            txc.TugSternName,
            txc.TugSternTime,
            txc.FirstLineTime,
            txc.LastLineTime,
            txc.DraftBow,
            txc.DraftMidship,
            txc.DraftStern,
            txc.Side,
            txc.Notes,
            txc.OccurredAt,
            txc.UndockingTime,
            photoUrls,
            TransmissionStatus.NotTransmitted);

        await _operations.AddAsync(operation, ct);

        // Stage 4 — Post-processing (nothing in MVP; status NotTransmitted)
        return Result<OperationResult>.Ok(ToResult(operation));
    }

    internal static OperationResult ToResult(Operation operation) =>
        new(
            operation.Id, operation.Type, operation.ShipId, operation.PortId, operation.BerthId,
            operation.AgencyName, operation.PilotName, operation.PilotBoardingTime,
            operation.TugBowName, operation.TugBowTime, operation.TugSternName, operation.TugSternTime,
            operation.FirstLineTime, operation.LastLineTime,
            operation.DraftBow, operation.DraftMidship, operation.DraftStern,
            operation.Side, operation.Notes, operation.OccurredAt, operation.UndockingTime,
            operation.Photos, operation.TransmissionStatus);
}
