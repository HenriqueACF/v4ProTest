using BksMarine.Core.Domain.Ports;
using BksMarine.Core.Domain.Users;

namespace BksMarine.Infrastructure.Auth;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public PasswordHash Hash(string plainPassword) =>
        new(BCrypt.Net.BCrypt.HashPassword(plainPassword));

    public bool Verify(string plainPassword, PasswordHash hash) =>
        BCrypt.Net.BCrypt.Verify(plainPassword, hash.Value);
}
