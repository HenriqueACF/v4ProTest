namespace BksMarine.Core.Domain.Operations;

public sealed record OperationReportRow(
    Guid OperationId,
    DateTime OccurredAt,
    OperationType Type,
    string ShipName,
    string PortName,
    string BerthName,
    TransmissionStatus TransmissionStatus,
    IReadOnlyList<string> Photos);
