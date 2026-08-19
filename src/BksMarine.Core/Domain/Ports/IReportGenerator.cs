using BksMarine.Core.Domain.Operations;

namespace BksMarine.Core.Domain.Ports;

public interface IReportGenerator
{
    Task<byte[]> GenerateAsync(OperationReportData data, CancellationToken ct = default);
}
