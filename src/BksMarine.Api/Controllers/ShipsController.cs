using BksMarine.Application.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BksMarine.Api.Controllers;

[ApiController]
[Route("ships")]
[Authorize(Policy = "configuration")]
public sealed class ShipsController : ControllerBase
{
    private readonly ICreateShip _create;
    private readonly IUpdateShip _update;
    private readonly IDeactivateShip _deactivate;
    private readonly IListShips _list;

    public ShipsController(ICreateShip create, IUpdateShip update, IDeactivateShip deactivate, IListShips list)
    {
        _create = create;
        _update = update;
        _deactivate = deactivate;
        _list = list;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List([FromQuery] bool activeOnly = true, CancellationToken ct = default)
    {
        var result = await _list.ExecuteAsync(activeOnly, ct);
        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateShipRequest request, CancellationToken ct = default)
    {
        var result = await _create.ExecuteAsync(new CreateShipTransaction(request.Name, request.Loa, request.Dwt), ct);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : MapError(result.Error!.Code, result.Error.Message);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateShipRequest request, CancellationToken ct = default)
    {
        var result = await _update.ExecuteAsync(new UpdateShipTransaction(id, request.Name, request.Loa, request.Dwt), ct);
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
        "validation.name" or "validation.loa" or "validation.dwt" => BadRequest(new { error = message }),
        "operations.ship_not_found" => NotFound(new { error = message }),
        _ => Conflict(new { error = message })
    };
}

public sealed record CreateShipRequest(string Name, decimal Loa, decimal Dwt);

public sealed record UpdateShipRequest(string Name, decimal Loa, decimal Dwt);
