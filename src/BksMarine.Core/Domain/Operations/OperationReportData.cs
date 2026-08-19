namespace BksMarine.Core.Domain.Operations;

public sealed record OperationReportData(
    DateTime? From,
    DateTime? To,
    OperationType? Type,
    Guid? PortId,
    IReadOnlyList<OperationReportRow> Rows);
