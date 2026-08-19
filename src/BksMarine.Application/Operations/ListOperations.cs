using BksMarine.Application.Common;
using BksMarine.Core.Domain.Operations;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Operations;

public interface IListOperations
{
    Task<Result<List<OperationResult>>> ExecuteAsync(OperationType? type = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
}

public sealed class ListOperations : IListOperations
{
    private readonly IOperationRepository _operations;

    public ListOperations(IOperationRepository operations) => _operations = operations;

    public async Task<Result<List<OperationResult>>> ExecuteAsync(OperationType? type = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var operations = await _operations.ListAsync(type, from, to, ct);
        return Result<List<OperationResult>>.Ok(operations.Select(RegisterOperation.ToResult).ToList());
    }
}
