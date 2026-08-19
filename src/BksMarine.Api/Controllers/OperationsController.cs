using BksMarine.Application.Operations;
using BksMarine.Application.Reports;
using BksMarine.Core.Domain.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BksMarine.Api.Controllers;

[ApiController]
[Route("operations")]
public sealed class OperationsController : ControllerBase
{
    private readonly IRegisterOperation _register;
    private readonly IListOperations _list;
    private readonly IGetOperation _get;
    private readonly IMarkTransmitted _transmit;
    private readonly IGenerateOperationReport _report;

    public OperationsController(
        IRegisterOperation register,
        IListOperations list,
        IGetOperation get,
        IMarkTransmitted transmit,
        IGenerateOperationReport report)
    {
        _register = register;
        _list = list;
        _get = get;
        _transmit = transmit;
        _report = report;
    }

    [HttpGet("report")]
    [Authorize]
    public async Task<IActionResult> Report(
        [FromQuery] OperationType? type = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] Guid? portId = null,
        CancellationToken ct = default)
    {
        var result = await _report.ExecuteAsync(type, from, to, portId, ct);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error!.Message });
        return File(result.Value!.Content, "application/pdf", result.Value.FileName);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> List(
        [FromQuery] OperationType? type = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var result = await _list.ExecuteAsync(type, from, to, ct);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct = default)
    {
        var result = await _get.ExecuteAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error!.Message });
    }

    [HttpPost]
    [Authorize(Policy = "configuration")]
    public async Task<IActionResult> Register(RegisterOperationRequest request, CancellationToken ct = default)
    {
        var result = await _register.ExecuteAsync(ToTransaction(request), ct);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : MapError(result.Error!.Code, result.Error.Message);
    }

    [HttpPost("{id:guid}/transmit")]
    [Authorize(Policy = "configuration")]
    public async Task<IActionResult> Transmit(Guid id, CancellationToken ct = default)
    {
        var result = await _transmit.ExecuteAsync(id, ct);
        return result.IsSuccess ? Ok() : MapError(result.Error!.Code, result.Error.Message);
    }

    private static RegisterOperationTransaction ToTransaction(RegisterOperationRequest r) =>
        new(
            r.Type, r.ShipId, r.PortId, r.BerthId, r.AgencyName, r.PilotName, r.PilotBoardingTime,
            r.TugBowName, r.TugBowTime, r.TugSternName, r.TugSternTime,
            r.FirstLineTime, r.LastLineTime, r.DraftBow, r.DraftMidship, r.DraftStern,
            r.Side, r.Notes, r.OccurredAt, r.UndockingTime, r.Photos ?? Array.Empty<string>());

    private IActionResult MapError(string code, string message) => code switch
    {
        "validation.draft_bow" or "validation.draft_midship" or "validation.draft_stern"
            or "validation.undocking_time" => BadRequest(new { error = message }),
        "operations.not_found" => NotFound(new { error = message }),
        _ => Conflict(new { error = message })
    };
}

public sealed record RegisterOperationRequest(
    OperationType Type,
    Guid ShipId,
    Guid PortId,
    Guid BerthId,
    string? AgencyName,
    string? PilotName,
    DateTime? PilotBoardingTime,
    string? TugBowName,
    DateTime? TugBowTime,
    string? TugSternName,
    DateTime? TugSternTime,
    DateTime? FirstLineTime,
    DateTime? LastLineTime,
    decimal? DraftBow,
    decimal? DraftMidship,
    decimal? DraftStern,
    Side? Side,
    string? Notes,
    DateTime OccurredAt,
    DateTime? UndockingTime,
    IReadOnlyList<string>? Photos);
