using System.Security.Claims;
using BksMarine.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BksMarine.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthenticateUser _authenticate;
    private readonly IRefreshSession _refreshSession;
    private readonly ILogoutSession _logoutSession;
    private readonly IResetPassword _resetPassword;

    public AuthController(
        IAuthenticateUser authenticate,
        IRefreshSession refreshSession,
        ILogoutSession logoutSession,
        IResetPassword resetPassword)
    {
        _authenticate = authenticate;
        _refreshSession = refreshSession;
        _logoutSession = logoutSession;
        _resetPassword = resetPassword;
    }

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
                "auth.throttled" => StatusCode(StatusCodes.Status429TooManyRequests, new { error = result.Error.Message }),
                _ => Unauthorized(new { error = result.Error.Message })
            };
        }

        return Ok(ToAuthResponse(result.Value!));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken ct)
    {
        var result = await _refreshSession.ExecuteAsync(new RefreshTransaction(request.RefreshToken), ct);
        return result.IsSuccess
            ? Ok(ToAuthResponse(result.Value!))
            : result.Error!.Code == "validation.refresh"
                ? BadRequest(new { error = result.Error.Message })
                : Unauthorized(new { error = result.Error.Message });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken ct)
    {
        var result = await _logoutSession.ExecuteAsync(new LogoutTransaction(request.RefreshToken), ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error!.Message });
    }

    [HttpPost("reset-password")]
    [Authorize]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue("userId");
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await _resetPassword.ExecuteAsync(userId, new ResetPasswordTransaction(request.CurrentPassword, request.NewPassword), ct);
        return result.IsFailure
            ? result.Error!.Code switch
            {
                "validation.new_password" or "validation.current_password" => BadRequest(new { error = result.Error.Message }),
                _ => Unauthorized(new { error = result.Error.Message })
            }
            : NoContent();
    }

    private static object ToAuthResponse(AuthenticationResult r) => new
    {
        token = r.Token,
        expiresAt = r.ExpiresAt,
        refreshToken = r.RefreshToken,
        refreshExpiresAt = r.RefreshExpiresAt,
        profile = r.Profile,
        menu = r.Menu
    };
}

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record ResetPasswordRequest(string CurrentPassword, string NewPassword);
