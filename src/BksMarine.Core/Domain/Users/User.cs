namespace BksMarine.Core.Domain.Users;

public sealed class User
{
    public Guid Id { get; }
    public string Name { get; }
    public string? JobTitle { get; }
    public Email Email { get; }
    public PasswordHash PasswordHash { get; }
    public Guid ProfileId { get; }
    public bool IsActive { get; }

    public User(
        Guid id,
        string name,
        string? jobTitle,
        Email email,
        PasswordHash passwordHash,
        Guid profileId,
        bool isActive)
    {
        Id = id;
        Name = name;
        JobTitle = jobTitle;
        Email = email;
        PasswordHash = passwordHash;
        ProfileId = profileId;
        IsActive = isActive;
    }
}
