namespace BksMarine.Core.Domain.Locations;

public sealed class Port
{
    public Guid Id { get; }
    public string Name { get; }
    public PortCode Code { get; }
    public string? Address { get; }
    public string? Contact { get; }
    public string? Notes { get; }
    public bool IsActive { get; }

    public Port(Guid id, string name, PortCode code, string? address, string? contact, string? notes, bool isActive)
    {
        Id = id;
        Name = name;
        Code = code;
        Address = address;
        Contact = contact;
        Notes = notes;
        IsActive = isActive;
    }
}
