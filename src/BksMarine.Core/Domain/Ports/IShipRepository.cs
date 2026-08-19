using BksMarine.Core.Domain.Operations;

namespace BksMarine.Core.Domain.Ports;

public interface IShipRepository
{
    Task<Ship?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Ship?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<List<Ship>> ListAsync(bool activeOnly, CancellationToken ct = default);
    Task AddAsync(Ship ship, CancellationToken ct = default);
    Task UpdateAsync(Ship ship, CancellationToken ct = default);
}
