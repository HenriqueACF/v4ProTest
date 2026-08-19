using BksMarine.Application.Common;
using BksMarine.Core.Domain.Operations;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Operations;

public interface IListOperations
{
    Task<Result<PageResult<OperationResult>>> ExecuteAsync(OperationType? type = null, DateTime? from = null, DateTime? to = null, int? page = null, int? pageSize = null, CancellationToken ct = default);
}

public sealed class ListOperations : IListOperations
{
    private readonly IOperationRepository _operations;

    public ListOperations(IOperationRepository operations) => _operations = operations;

    public async Task<Result<PageResult<OperationResult>>> ExecuteAsync(OperationType? type = null, DateTime? from = null, DateTime? to = null, int? page = null, int? pageSize = null, CancellationToken ct = default)
    {
        var (p, ps) = Paging.Normalize(page, pageSize);
        var total = await _operations.CountAsync(type, from, to, ct);
        var operations = await _operations.ListAsync(type, from, to, p, ps, ct);
        return Result<PageResult<OperationResult>>.Ok(new PageResult<OperationResult>(
            operations.Select(RegisterOperation.ToResult).ToList(), p, ps, total));
    }
}
