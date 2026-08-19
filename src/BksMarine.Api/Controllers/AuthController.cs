using BksMarine.Application.Auth;
using Microsoft.AspNetCore.Mvc;

namespace BksMarine.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthenticateUser _authenticate;

    public AuthController(IAuthenticateUser authenticate) => _authenticate = authenticate;

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await _authenticate.AuthenticateAsync(
            new AuthenticateTransaction(request.Email, request.Password), ct);

        if (result.IsFailure)
        {
            return result.Error!.Code switch
            {
                "validation.email" or "validation.password" => BadRequest(new { error = result.Error.Message }),
                _ => Unauthorized(new { error = result.Error.Message })
            };
        }

        return Ok(new
        {
            token = result.Value!.Token,
            expiresAt = result.Value.ExpiresAt,
            profile = result.Value.Profile,
            menu = result.Value.Menu
        });
    }
}

public sealed record LoginRequest(string Email, string Password);
