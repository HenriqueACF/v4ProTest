using BksMarine.Application.Employees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BksMarine.Api.Controllers;

[ApiController]
[Route("employees")]
public sealed class EmployeesController : ControllerBase
{
    private readonly ICreateEmployee _create;
    private readonly IUpdateEmployee _update;
    private readonly IDeactivateEmployee _deactivate;
    private readonly IListEmployees _list;

    public EmployeesController(
        ICreateEmployee create,
        IUpdateEmployee update,
        IDeactivateEmployee deactivate,
        IListEmployees list)
    {
        _create = create;
        _update = update;
        _deactivate = deactivate;
        _list = list;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> List([FromQuery] bool activeOnly = true, CancellationToken ct = default)
    {
        var result = await _list.ExecuteAsync(activeOnly, ct);
        return Ok(result.Value);
    }

    [HttpPost]
    [Authorize(Policy = "configuration")]
    public async Task<IActionResult> Create(CreateEmployeeRequest request, CancellationToken ct = default)
    {
        var result = await _create.ExecuteAsync(
            new CreateEmployeeTransaction(request.Name, request.Email, request.Password, request.ProfileId, request.JobTitle), ct);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : MapError(result.Error!.Code, result.Error.Message);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "configuration")]
    public async Task<IActionResult> Update(Guid id, UpdateEmployeeRequest request, CancellationToken ct = default)
    {
        var result = await _update.ExecuteAsync(
            new UpdateEmployeeTransaction(id, request.Name, request.ProfileId, request.JobTitle), ct);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error!.Code, result.Error.Message);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "configuration")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct = default)
    {
        var result = await _deactivate.ExecuteAsync(id, ct);
        return result.IsSuccess ? NoContent() : MapError(result.Error!.Code, result.Error.Message);
    }

    private IActionResult MapError(string code, string message) => code switch
    {
        "validation.name" or "validation.email" or "validation.password" or "validation.profile"
            => BadRequest(new { error = message }),
        "employees.not_found" => NotFound(new { error = message }),
        _ => Conflict(new { error = message })
    };
}

public sealed record CreateEmployeeRequest(string Name, string Email, string Password, Guid ProfileId, string? JobTitle);

public sealed record UpdateEmployeeRequest(string Name, Guid ProfileId, string? JobTitle);
