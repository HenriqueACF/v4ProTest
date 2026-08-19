using BksMarine.Application.Locations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BksMarine.Api.Controllers;

[ApiController]
[Route("berths")]
[Authorize(Policy = "configuration")]
public sealed class BerthsController : ControllerBase
{
    private readonly IUpdateBerth _update;
    private readonly IDeactivateBerth _deactivate;

    public BerthsController(IUpdateBerth update, IDeactivateBerth deactivate)
    {
        _update = update;
        _deactivate = deactivate;
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateBerthRequest request, CancellationToken ct = default)
    {
        var result = await _update.ExecuteAsync(
            new UpdateBerthTransaction(id, request.Name, request.MaxLoa, request.MaxDwt, request.Type, request.Notes), ct);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error!.Code, result.Error.Message);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct = default)
    {
        var result = await _deactivate.ExecuteAsync(id, ct);
        return result.IsSuccess ? NoContent() : MapError(result.Error!.Code, result.Error.Message);
    }

    private IActionResult MapError(string code, string message) => code switch
    {
        "validation.name" or "validation.max_loa" or "validation.max_dwt" or "validation.type"
            => BadRequest(new { error = message }),
        "locations.berth.not_found" => NotFound(new { error = message }),
        _ => Conflict(new { error = message })
    };
}

public sealed record UpdateBerthRequest(string Name, decimal? MaxLoa, decimal? MaxDwt, Core.Domain.Locations.BerthType Type, string? Notes);
