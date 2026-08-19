using BksMarine.Application.Locations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BksMarine.Api.Controllers;

[ApiController]
[Route("ports")]
public sealed class PortsController : ControllerBase
{
    private const string ConfigurationPolicy = "configuration";

    private readonly ICreatePort _create;
    private readonly IUpdatePort _update;
    private readonly IDeactivatePort _deactivate;
    private readonly IListPorts _list;
    private readonly ICreateBerth _createBerth;
    private readonly IListBerthsByPort _listBerths;

    public PortsController(
        ICreatePort create,
        IUpdatePort update,
        IDeactivatePort deactivate,
        IListPorts list,
        ICreateBerth createBerth,
        IListBerthsByPort listBerths)
    {
        _create = create;
        _update = update;
        _deactivate = deactivate;
        _list = list;
        _createBerth = createBerth;
        _listBerths = listBerths;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> List(
        [FromQuery] bool activeOnly = true,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        CancellationToken ct = default)
    {
        var result = await _list.ExecuteAsync(activeOnly, page, pageSize, ct);
        return Ok(result.Value);
    }

    [HttpPost]
    [Authorize(Policy = ConfigurationPolicy)]
    public async Task<IActionResult> Create(CreatePortRequest request, CancellationToken ct = default)
    {
        var result = await _create.ExecuteAsync(
            new CreatePortTransaction(request.Name, request.Code, request.Address, request.Contact, request.Notes), ct);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : MapError(result.Error!.Code, result.Error.Message);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = ConfigurationPolicy)]
    public async Task<IActionResult> Update(Guid id, UpdatePortRequest request, CancellationToken ct = default)
    {
        var result = await _update.ExecuteAsync(
            new UpdatePortTransaction(id, request.Name, request.Code, request.Address, request.Contact, request.Notes), ct);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error!.Code, result.Error.Message);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = ConfigurationPolicy)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct = default)
    {
        var result = await _deactivate.ExecuteAsync(id, ct);
        return result.IsSuccess ? NoContent() : MapError(result.Error!.Code, result.Error.Message);
    }

    [HttpGet("{portId:guid}/berths")]
    [Authorize]
    public async Task<IActionResult> ListBerths(
        Guid portId,
        [FromQuery] bool activeOnly = true,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        CancellationToken ct = default)
    {
        var result = await _listBerths.ExecuteAsync(portId, activeOnly, page, pageSize, ct);
        return Ok(result.Value);
    }

    [HttpPost("{portId:guid}/berths")]
    [Authorize(Policy = ConfigurationPolicy)]
    public async Task<IActionResult> CreateBerth(Guid portId, CreateBerthRequest request, CancellationToken ct = default)
    {
        var result = await _createBerth.ExecuteAsync(
            new CreateBerthTransaction(request.Name, portId, request.MaxLoa, request.MaxDwt, request.Type, request.Notes), ct);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : MapError(result.Error!.Code, result.Error.Message);
    }

    private IActionResult MapError(string code, string message) => code switch
    {
        "validation.name" or "validation.code" or "validation.max_loa" or "validation.max_dwt" or "validation.type"
            => BadRequest(new { error = message }),
        "locations.port.not_found" or "locations.berth.not_found" => NotFound(new { error = message }),
        _ => Conflict(new { error = message })
    };
}

public sealed record CreatePortRequest(string Name, string Code, string? Address, string? Contact, string? Notes);

public sealed record UpdatePortRequest(string Name, string Code, string? Address, string? Contact, string? Notes);

public sealed record CreateBerthRequest(string Name, decimal? MaxLoa, decimal? MaxDwt, Core.Domain.Locations.BerthType Type, string? Notes);
