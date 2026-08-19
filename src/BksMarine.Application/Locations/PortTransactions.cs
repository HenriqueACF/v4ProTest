namespace BksMarine.Application.Locations;

public sealed record CreatePortTransaction(string Name, string Code, string? Address, string? Contact, string? Notes);

public sealed record UpdatePortTransaction(Guid Id, string Name, string Code, string? Address, string? Contact, string? Notes);

public sealed record PortResult(Guid Id, string Name, string Code, string? Address, string? Contact, string? Notes, bool IsActive);
