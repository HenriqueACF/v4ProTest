using BksMarine.Core.Domain.Locations;

namespace BksMarine.Application.Locations;

public sealed record CreateBerthTransaction(
    string Name,
    Guid PortId,
    decimal? MaxLoa,
    decimal? MaxDwt,
    BerthType Type,
    string? Notes);

public sealed record UpdateBerthTransaction(
    Guid Id,
    string Name,
    decimal? MaxLoa,
    decimal? MaxDwt,
    BerthType Type,
    string? Notes);

public sealed record BerthResult(
    Guid Id,
    string Name,
    Guid PortId,
    decimal? MaxLoa,
    decimal? MaxDwt,
    BerthType Type,
    string? Notes,
    bool IsActive);
