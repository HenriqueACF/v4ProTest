namespace BksMarine.Core.Domain.Operations;

public sealed class Operation
{
    public Guid Id { get; }
    public OperationType Type { get; }
    public Guid ShipId { get; }
    public Guid PortId { get; }
    public Guid BerthId { get; }
    public string? AgencyName { get; }
    public string? PilotName { get; }
    public DateTime? PilotBoardingTime { get; }
    public string? TugBowName { get; }
    public DateTime? TugBowTime { get; }
    public string? TugSternName { get; }
    public DateTime? TugSternTime { get; }
    public DateTime? FirstLineTime { get; }
    public DateTime? LastLineTime { get; }
    public decimal? DraftBow { get; }
    public decimal? DraftMidship { get; }
    public decimal? DraftStern { get; }
    public Side? Side { get; }
    public string? Notes { get; }
    public DateTime OccurredAt { get; }
    public DateTime? UndockingTime { get; }
    public IReadOnlyList<string> Photos { get; }
    public TransmissionStatus TransmissionStatus { get; }
    public DateTime CreatedAt { get; }

    public Operation(
        Guid id,
        OperationType type,
        Guid shipId,
        Guid portId,
        Guid berthId,
        string? agencyName,
        string? pilotName,
        DateTime? pilotBoardingTime,
        string? tugBowName,
        DateTime? tugBowTime,
        string? tugSternName,
        DateTime? tugSternTime,
        DateTime? firstLineTime,
        DateTime? lastLineTime,
        decimal? draftBow,
        decimal? draftMidship,
        decimal? draftStern,
        Side? side,
        string? notes,
        DateTime occurredAt,
        DateTime? undockingTime,
        IReadOnlyList<string> photos,
        TransmissionStatus transmissionStatus,
        DateTime? createdAt = null)
    {
        Id = id;
        Type = type;
        ShipId = shipId;
        PortId = portId;
        BerthId = berthId;
        AgencyName = agencyName;
        PilotName = pilotName;
        PilotBoardingTime = pilotBoardingTime;
        TugBowName = tugBowName;
        TugBowTime = tugBowTime;
        TugSternName = tugSternName;
        TugSternTime = tugSternTime;
        FirstLineTime = firstLineTime;
        LastLineTime = lastLineTime;
        DraftBow = draftBow;
        DraftMidship = draftMidship;
        DraftStern = draftStern;
        Side = side;
        Notes = notes;
        OccurredAt = occurredAt;
        UndockingTime = undockingTime;
        Photos = photos;
        TransmissionStatus = transmissionStatus;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }
}
