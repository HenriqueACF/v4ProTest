using BksMarine.Application.Employees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BksMarine.Api.Controllers;

[ApiController]
[Route("profiles")]
public sealed class ProfilesController : ControllerBase
{
    private readonly IListProfiles _list;

    public ProfilesController(IListProfiles list) => _list = list;

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> List(CancellationToken ct = default)
    {
        var result = await _list.ExecuteAsync(ct);
        return Ok(result.Value);
    }
}
