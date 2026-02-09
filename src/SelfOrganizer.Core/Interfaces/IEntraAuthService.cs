namespace SelfOrganizer.Core.Interfaces;

/// <summary>
/// Service for Microsoft Entra (Azure AD) authentication.
/// Supports feature toggle for optional authentication in deployed environments.
/// </summary>
public interface IEntraAuthService
{
    /// <summary>
    /// Gets whether Entra authentication is enabled via feature toggle.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets the current authentication state.
    /// </summary>
    AuthState State { get; }

    /// <summary>
    /// Gets the current user info if authenticated.
    /// </summary>
    EntraUser? CurrentUser { get; }

    /// <summary>
    /// Event raised when authentication state changes.
    /// </summary>
    event Action<AuthState>? OnAuthStateChanged;

    /// <summary>
    /// Event raised when user info is updated.
    /// </summary>
    event Action<EntraUser?>? OnUserChanged;

    /// <summary>
    /// Checks for existing session on app load.
    /// </summary>
    Task<bool> CheckSessionAsync();

    /// <summary>
    /// Initiates the login flow (redirect to Entra).
    /// </summary>
    Task<AuthResult> LoginAsync();

    /// <summary>
    /// Handles the OAuth callback after redirect.
    /// </summary>
    Task<AuthResult> HandleCallbackAsync(string code, string state);

    /// <summary>
    /// Logs the user out.
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// Gets the current access token for API calls.
    /// Will refresh if expired.
    /// </summary>
    Task<string?> GetAccessTokenAsync();

    /// <summary>
    /// Gets a token for a specific scope (e.g., Microsoft Graph).
    /// </summary>
    Task<string?> GetAccessTokenAsync(string[] scopes);

    /// <summary>
    /// Checks if the current token is valid.
    /// </summary>
    Task<bool> IsTokenValidAsync();

    /// <summary>
    /// Refreshes the access token.
    /// </summary>
    Task<AuthResult> RefreshTokenAsync();
}

/// <summary>
/// Authentication state.
/// </summary>
public enum AuthState
{
    /// <summary>
    /// Auth feature is disabled.
    /// </summary>
    Disabled,

    /// <summary>
    /// Checking for existing session.
    /// </summary>
    Checking,

    /// <summary>
    /// User is not authenticated.
    /// </summary>
    Unauthenticated,

    /// <summary>
    /// Authentication is in progress.
    /// </summary>
    Authenticating,

    /// <summary>
    /// User is authenticated.
    /// </summary>
    Authenticated,

    /// <summary>
    /// Token is being refreshed.
    /// </summary>
    Refreshing,

    /// <summary>
    /// Authentication error occurred.
    /// </summary>
    Error
}

/// <summary>
/// Result of an authentication operation.
/// </summary>
public class AuthResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? ErrorDescription { get; set; }
    public EntraUser? User { get; set; }
    public string? AccessToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Entra user information.
/// </summary>
public class EntraUser
{
    /// <summary>
    /// The user's unique object ID in Entra.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The user's display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The user's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The user's user principal name.
    /// </summary>
    public string UserPrincipalName { get; set; } = string.Empty;

    /// <summary>
    /// The user's first name.
    /// </summary>
    public string? GivenName { get; set; }

    /// <summary>
    /// The user's last name.
    /// </summary>
    public string? Surname { get; set; }

    /// <summary>
    /// URL to the user's profile photo.
    /// </summary>
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// The tenant ID.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// User's roles/groups if available.
    /// </summary>
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// Configuration for Entra authentication.
/// </summary>
public class EntraAuthConfig
{
    /// <summary>
    /// Whether Entra auth is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// The Entra application (client) ID.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The Entra tenant ID.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// The redirect URI after authentication.
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// The scopes to request.
    /// </summary>
    public List<string> Scopes { get; set; } = new() { "openid", "profile", "email", "User.Read" };

    /// <summary>
    /// Additional scopes for Microsoft Graph calendar access.
    /// </summary>
    public List<string> CalendarScopes { get; set; } = new() { "Calendars.Read", "Calendars.ReadWrite" };
}
