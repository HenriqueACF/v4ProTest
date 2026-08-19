using BksMarine.Core.Domain.Profiles;

namespace BksMarine.Application.Employees;

public sealed record CreateEmployeeTransaction(string Name, string Email, string Password, Guid ProfileId, string? JobTitle);

public sealed record UpdateEmployeeTransaction(Guid Id, string Name, Guid ProfileId, string? JobTitle);

public sealed record EmployeeResult(Guid Id, string Name, string Email, string? JobTitle, ProfileName Profile, bool IsActive);

public sealed record ProfileResult(Guid Id, ProfileName Name, IReadOnlyCollection<Module> AllowedModules);
