using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SelfOrganizer.Server.Services.Auth;

namespace SelfOrganizer.Server.Controllers;

/// <summary>
/// Controller for authentication endpoints.
/// Handles login, logout, and user info requests.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IServerAuthService _authService;

    public AuthController(IServerAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Initiates the login flow by redirecting to Entra.
    /// </summary>
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? returnUrl = "/")
    {
        var redirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : "/";

        return Challenge(
            new AuthenticationProperties
            {
                RedirectUri = redirectUri
            },
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Logs out the user and redirects to Entra signout.
    /// </summary>
    [HttpGet("logout")]
    public IActionResult Logout()
    {
        return SignOut(
            new AuthenticationProperties
            {
                RedirectUri = "/"
            },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Gets the current authenticated user's information.
    /// Returns 401 if not authenticated.
    /// </summary>
    [HttpGet("user")]
    public ActionResult<UserInfoDto> GetUser()
    {
        if (!_authService.IsAuthenticated)
        {
            return Unauthorized();
        }

        return Ok(new UserInfoDto
        {
            Id = _authService.GetCurrentUserId(),
            Name = _authService.GetCurrentUserName(),
            Email = _authService.GetCurrentUserEmail(),
            IsAuthenticated = true
        });
    }

    /// <summary>
    /// Checks if the user is authenticated.
    /// Returns authentication status without requiring auth.
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    public ActionResult<AuthStatusDto> GetStatus()
    {
        return Ok(new AuthStatusDto
        {
            IsAuthenticated = _authService.IsAuthenticated,
            UserId = _authService.GetCurrentUserId(),
            UserName = _authService.GetCurrentUserName()
        });
    }
}

/// <summary>
/// DTO for user information.
/// </summary>
public class UserInfoDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public bool IsAuthenticated { get; set; }
}

/// <summary>
/// DTO for authentication status.
/// </summary>
public class AuthStatusDto
{
    public bool IsAuthenticated { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
}
