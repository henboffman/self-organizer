using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;

namespace SelfOrganizer.Server.Services.Auth;

/// <summary>
/// Server-side authentication service that extracts user information from the HTTP context.
/// </summary>
public interface IServerAuthService
{
    /// <summary>
    /// Gets the current authenticated user's Object ID (oid claim from Entra).
    /// Returns null if not authenticated.
    /// </summary>
    string? GetCurrentUserId();

    /// <summary>
    /// Gets the current user's email address.
    /// </summary>
    string? GetCurrentUserEmail();

    /// <summary>
    /// Gets the current user's display name.
    /// </summary>
    string? GetCurrentUserName();

    /// <summary>
    /// Gets an access token for calling downstream APIs (e.g., Microsoft Graph).
    /// </summary>
    Task<string?> GetAccessTokenAsync(string[] scopes);

    /// <summary>
    /// Returns true if the current request is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}

/// <summary>
/// Implementation of IServerAuthService using HttpContext and Microsoft Identity Web.
/// </summary>
public class ServerAuthService : IServerAuthService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenAcquisition? _tokenAcquisition;
    private readonly IConfiguration _configuration;

    // Default user ID for development/testing when Azure AD is not configured
    private const string DevelopmentUserId = "dev-test-user-00000000-0000-0000-0000-000000000001";
    private const string DevelopmentUserEmail = "dev@localhost";
    private const string DevelopmentUserName = "Development User";

    public ServerAuthService(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ITokenAcquisition? tokenAcquisition = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _tokenAcquisition = tokenAcquisition;
    }

    private bool IsAzureAdConfigured =>
        !string.IsNullOrEmpty(_configuration["AzureAd:ClientId"]);

    public bool IsAuthenticated =>
        (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false)
        || !IsAzureAdConfigured; // Always "authenticated" in dev mode without Azure AD

    public string? GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            // Try to get Object ID (oid) claim - this is the unique user identifier in Entra
            return user.FindFirst("oid")?.Value
                ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }

        // Return development user ID when Azure AD is not configured
        if (!IsAzureAdConfigured)
        {
            return DevelopmentUserId;
        }

        return null;
    }

    public string? GetCurrentUserEmail()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            return user.FindFirst("preferred_username")?.Value
                ?? user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                ?? user.FindFirst("email")?.Value;
        }

        // Return development email when Azure AD is not configured
        if (!IsAzureAdConfigured)
        {
            return DevelopmentUserEmail;
        }

        return null;
    }

    public string? GetCurrentUserName()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            return user.FindFirst("name")?.Value
                ?? user.Identity?.Name;
        }

        // Return development name when Azure AD is not configured
        if (!IsAzureAdConfigured)
        {
            return DevelopmentUserName;
        }

        return null;
    }

    public async Task<string?> GetAccessTokenAsync(string[] scopes)
    {
        if (_tokenAcquisition == null)
        {
            return null;
        }

        try
        {
            return await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);
        }
        catch
        {
            return null;
        }
    }
}
