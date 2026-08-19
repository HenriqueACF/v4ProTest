using BksMarine.Core.Domain.Operations;

namespace BksMarine.Core.Domain.Ports;

public interface IOperationRepository
{
    Task<Operation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Operation>> ListAsync(OperationType? type, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAsync(OperationType? type, DateTime? from, DateTime? to, CancellationToken ct = default);
    Task<List<OperationReportRow>> ListReportAsync(OperationType? type, DateTime? from, DateTime? to, Guid? portId, Guid? responsibleUserId, CancellationToken ct = default);
    Task AddAsync(Operation operation, CancellationToken ct = default);
    Task UpdateAsync(Operation operation, CancellationToken ct = default);
}
