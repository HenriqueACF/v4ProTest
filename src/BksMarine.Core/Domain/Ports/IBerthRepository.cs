using BksMarine.Core.Domain.Locations;

namespace BksMarine.Core.Domain.Ports;

public interface IBerthRepository
{
    Task<Berth?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Berth?> GetByNameInPortAsync(string name, Guid portId, CancellationToken ct = default);
    Task<List<Berth>> ListByPortAsync(Guid portId, bool activeOnly, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountByPortAsync(Guid portId, bool activeOnly, CancellationToken ct = default);
    Task AddAsync(Berth berth, CancellationToken ct = default);
    Task UpdateAsync(Berth berth, CancellationToken ct = default);
}
