using BksMarine.Application.Common;
using BksMarine.Core.Domain.Ports;

namespace BksMarine.Application.Employees;

public interface IListProfiles
{
    Task<Result<List<ProfileResult>>> ExecuteAsync(CancellationToken ct = default);
}

public sealed class ListProfiles : IListProfiles
{
    private readonly IUserRepository _users;

    public ListProfiles(IUserRepository users) => _users = users;

    public async Task<Result<List<ProfileResult>>> ExecuteAsync(CancellationToken ct = default)
    {
        var profiles = await _users.GetAllProfilesAsync(ct);
        return Result<List<ProfileResult>>.Ok(
            profiles.Select(p => new ProfileResult(p.Id, p.Name, p.AllowedModules)).ToList());
    }
}
