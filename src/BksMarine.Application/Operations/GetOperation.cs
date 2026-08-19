using BksMarine.Application.Common;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Operations;

public interface IGetOperation
{
    Task<Result<OperationResult>> ExecuteAsync(Guid id, CancellationToken ct = default);
}

public sealed class GetOperation : IGetOperation
{
    private readonly IOperationRepository _operations;

    public GetOperation(IOperationRepository operations) => _operations = operations;

    public async Task<Result<OperationResult>> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var operation = await _operations.GetByIdAsync(id, ct);
        if (operation is null)
            return Result<OperationResult>.Fail(new Error("operations.not_found", "Operation not found."));
        return Result<OperationResult>.Ok(RegisterOperation.ToResult(operation));
    }
}
