namespace BksMarine.Core.Domain.Profiles;

public sealed class Profile
{
    public Guid Id { get; }
    public ProfileName Name { get; }
    public IReadOnlyCollection<Module> AllowedModules { get; }

    public Profile(Guid id, ProfileName name, IReadOnlyCollection<Module> allowedModules)
    {
        Id = id;
        Name = name;
        AllowedModules = allowedModules;
    }
}
