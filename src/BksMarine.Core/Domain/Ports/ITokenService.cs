using BksMarine.Core.Domain.Profiles;
using BksMarine.Core.Domain.Users;

namespace BksMarine.Core.Domain.Ports;

public interface ITokenService
{
    IssuedToken Issue(User user, Profile profile);
}
