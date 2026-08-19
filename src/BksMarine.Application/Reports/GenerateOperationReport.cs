using BksMarine.Application.Common;
using BksMarine.Core.Domain.Operations;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Reports;

public sealed record OperationReportFile(byte[] Content, string FileName);

public interface IGenerateOperationReport
{
    Task<Result<OperationReportFile>> ExecuteAsync(OperationType? type, DateTime? from, DateTime? to, Guid? portId, Guid? responsibleUserId, CancellationToken ct = default);
}

public sealed class GenerateOperationReport : IGenerateOperationReport
{
    private readonly IOperationRepository _operations;
    private readonly IReportGenerator _generator;

    public GenerateOperationReport(IOperationRepository operations, IReportGenerator generator)
    {
        _operations = operations;
        _generator = generator;
    }

    public async Task<Result<OperationReportFile>> ExecuteAsync(
        OperationType? type, DateTime? from, DateTime? to, Guid? portId, Guid? responsibleUserId, CancellationToken ct = default)
    {
        // Validation
        if (from is not null && to is not null && from > to)
            return Result<OperationReportFile>.Fail(new Error("validation.period", "Start date must be before end date."));

        // Processing (read model) + Post-processing (generate PDF)
        var rows = await _operations.ListReportAsync(type, from, to, portId, responsibleUserId, ct);
        var data = new OperationReportData(from, to, type, portId, responsibleUserId, rows);
        var pdf = await _generator.GenerateAsync(data, ct);

        return Result<OperationReportFile>.Ok(new OperationReportFile(pdf, $"relatorio-operacoes-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf"));
    }
}
