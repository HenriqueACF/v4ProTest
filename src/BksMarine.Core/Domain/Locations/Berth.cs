namespace BksMarine.Core.Domain.Locations;

public sealed class Berth
{
    public Guid Id { get; }
    public string Name { get; }
    public Guid PortId { get; }
    public decimal? MaxLoa { get; }
    public decimal? MaxDwt { get; }
    public BerthType Type { get; }
    public string? Notes { get; }
    public bool IsActive { get; }

    public Berth(Guid id, string name, Guid portId, decimal? maxLoa, decimal? maxDwt, BerthType type, string? notes, bool isActive)
    {
        Id = id;
        Name = name;
        PortId = portId;
        MaxLoa = maxLoa;
        MaxDwt = maxDwt;
        Type = type;
        Notes = notes;
        IsActive = isActive;
    }
}
