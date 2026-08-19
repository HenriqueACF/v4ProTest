using BksMarine.Core.Domain.Users;

namespace BksMarine.Core.Domain.Ports;

public interface IUserRepository
{
    Task<UserAccount?> GetByEmailAsync(Email email, CancellationToken ct = default);
}
