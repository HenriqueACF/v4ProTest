using BksMarine.Core.Domain.Profiles;

namespace BksMarine.Core.Domain.Users;

public sealed class UserAccount
{
    public User User { get; }
    public Profile Profile { get; }

    public UserAccount(User user, Profile profile)
    {
        User = user;
        Profile = profile;
    }
}
