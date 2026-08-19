using BksMarine.Core.Domain.Profiles;
using BksMarine.Core.Domain.Users;

namespace BksMarine.Core.Domain.Ports;

public interface IUserRepository
{
    Task<UserAccount?> GetByEmailAsync(Email email, CancellationToken ct = default);
    Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<UserAccount>> ListAsync(bool activeOnly, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAsync(bool activeOnly, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task UpdatePasswordAsync(Guid userId, PasswordHash hash, CancellationToken ct = default);
    Task<Profile?> GetProfileByIdAsync(Guid profileId, CancellationToken ct = default);
    Task<List<Profile>> GetAllProfilesAsync(CancellationToken ct = default);
}
