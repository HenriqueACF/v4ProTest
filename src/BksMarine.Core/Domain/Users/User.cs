namespace BksMarine.Core.Domain.Users;

public sealed class User
{
    public Guid Id { get; }
    public Email Email { get; }
    public PasswordHash PasswordHash { get; }
    public Guid ProfileId { get; }
    public bool IsActive { get; }

    public User(Guid id, Email email, PasswordHash passwordHash, Guid profileId, bool isActive)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        ProfileId = profileId;
        IsActive = isActive;
    }
}
