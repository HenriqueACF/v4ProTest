using BksMarine.Core.Domain.Locations;

namespace BksMarine.Core.Domain.Ports;

public interface IPortRepository
{
    Task<Port?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Port?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<List<Port>> ListAsync(bool activeOnly, CancellationToken ct = default);
    Task AddAsync(Port port, CancellationToken ct = default);
    Task UpdateAsync(Port port, CancellationToken ct = default);
}
