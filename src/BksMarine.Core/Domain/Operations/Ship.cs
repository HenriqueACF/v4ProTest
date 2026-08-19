namespace BksMarine.Core.Domain.Operations;

public sealed class Ship
{
    public Guid Id { get; }
    public string Name { get; }
    public decimal Loa { get; }
    public decimal Dwt { get; }
    public bool IsActive { get; }

    public Ship(Guid id, string name, decimal loa, decimal dwt, bool isActive)
    {
        Id = id;
        Name = name;
        Loa = loa;
        Dwt = dwt;
        IsActive = isActive;
    }
}
