using BksMarine.Core.Domain.Users;

namespace BksMarine.Core.Domain.Ports;

public interface IPasswordHasher
{
    PasswordHash Hash(string plainPassword);
    bool Verify(string plainPassword, PasswordHash hash);
}
