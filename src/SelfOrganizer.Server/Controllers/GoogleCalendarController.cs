using Microsoft.AspNetCore.Mvc;
using SelfOrganizer.Server.Services;

namespace SelfOrganizer.Server.Controllers;

[ApiController]
[Route("api/google-calendar")]
public class GoogleCalendarController : ControllerBase
{
    private readonly IGoogleCalendarProxyService _googleCalendarService;

    public GoogleCalendarController(IGoogleCalendarProxyService googleCalendarService)
    {
        _googleCalendarService = googleCalendarService;
    }

    /// <summary>
    /// Check if Google Calendar is configured on the server
    /// </summary>
    [HttpGet("status")]
    public ActionResult<GoogleCalendarStatusResponse> GetStatus()
    {
        return Ok(new GoogleCalendarStatusResponse
        {
            IsConfigured = _googleCalendarService.IsConfigured()
        });
    }

    /// <summary>
    /// Generate OAuth authorization URL
    /// </summary>
    [HttpPost("auth-url")]
    public ActionResult<GoogleAuthUrlResponse> GenerateAuthUrl([FromBody] GenerateAuthUrlRequest request)
    {
        var response = _googleCalendarService.GenerateAuthUrl(request.State ?? Guid.NewGuid().ToString());
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    /// <summary>
    /// Exchange authorization code for tokens
    /// </summary>
    [HttpPost("token")]
    public async Task<ActionResult<GoogleTokenResponse>> ExchangeCode([FromBody] ExchangeCodeRequest request)
    {
        if (string.IsNullOrEmpty(request.Code) || string.IsNullOrEmpty(request.CodeVerifier))
        {
            return BadRequest(new GoogleTokenResponse
            {
                Success = false,
                Error = "Code and CodeVerifier are required"
            });
        }

        var response = await _googleCalendarService.ExchangeCodeAsync(request.Code, request.CodeVerifier);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<GoogleTokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return BadRequest(new GoogleTokenResponse
            {
                Success = false,
                Error = "RefreshToken is required"
            });
        }

        var response = await _googleCalendarService.RefreshTokenAsync(request.RefreshToken);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    /// <summary>
    /// Get list of calendars
    /// </summary>
    [HttpGet("calendars")]
    public async Task<ActionResult<GoogleCalendarListResponse>> GetCalendars([FromHeader(Name = "Authorization")] string authorization)
    {
        var accessToken = ExtractBearerToken(authorization);
        if (string.IsNullOrEmpty(accessToken))
        {
            return Unauthorized(new GoogleCalendarListResponse
            {
                Success = false,
                Error = "Authorization header with Bearer token required"
            });
        }

        var response = await _googleCalendarService.GetCalendarsAsync(accessToken);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    /// <summary>
    /// Get events for a calendar
    /// </summary>
    [HttpGet("events")]
    public async Task<ActionResult<GoogleEventsResponse>> GetEvents(
        [FromHeader(Name = "Authorization")] string authorization,
        [FromQuery] string calendarId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var accessToken = ExtractBearerToken(authorization);
        if (string.IsNullOrEmpty(accessToken))
        {
            return Unauthorized(new GoogleEventsResponse
            {
                Success = false,
                Error = "Authorization header with Bearer token required"
            });
        }

        if (string.IsNullOrEmpty(calendarId))
        {
            return BadRequest(new GoogleEventsResponse
            {
                Success = false,
                Error = "calendarId is required"
            });
        }

        var start = startDate ?? DateTime.UtcNow.AddDays(-7);
        var end = endDate ?? DateTime.UtcNow.AddDays(30);

        var response = await _googleCalendarService.GetEventsAsync(accessToken, calendarId, start, end);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    private static string? ExtractBearerToken(string? authorization)
    {
        if (string.IsNullOrEmpty(authorization))
            return null;

        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authorization.Substring(7).Trim();

        return authorization;
    }
}

public class GoogleCalendarStatusResponse
{
    public bool IsConfigured { get; set; }
}

public class GenerateAuthUrlRequest
{
    public string? State { get; set; }
}

public class ExchangeCodeRequest
{
    public string Code { get; set; } = string.Empty;
    public string CodeVerifier { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
