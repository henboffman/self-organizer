using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using SelfOrganizer.Core.Interfaces;

namespace SelfOrganizer.App.Services.Auth;

/// <summary>
/// Authentication state provider that delegates to the server.
/// In the hosted model, authentication is handled via HTTP-only cookies.
/// </summary>
public class ServerAuthenticationStateProvider : AuthenticationStateProvider, IEntraAuthService
{
    private readonly HttpClient _httpClient;
    private AuthState _state = AuthState.Checking;
    private EntraUser? _currentUser;

    private const string AuthStatusEndpoint = "api/auth/status";
    private const string AuthUserEndpoint = "api/auth/user";
    private const string AuthLoginEndpoint = "api/auth/login";
    private const string AuthLogoutEndpoint = "api/auth/logout";

    public bool IsEnabled => true; // Always enabled in hosted mode
    public AuthState State => _state;
    public EntraUser? CurrentUser => _currentUser;

    public event Action<AuthState>? OnAuthStateChanged;
    public event Action<EntraUser?>? OnUserChanged;

    public ServerAuthenticationStateProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(AuthUserEndpoint);

            if (response.IsSuccessStatusCode)
            {
                var userInfo = await response.Content.ReadFromJsonAsync<UserInfoDto>();

                if (userInfo?.IsAuthenticated == true)
                {
                    _currentUser = new EntraUser
                    {
                        Id = userInfo.Id ?? string.Empty,
                        DisplayName = userInfo.Name ?? string.Empty,
                        Email = userInfo.Email ?? string.Empty,
                        UserPrincipalName = userInfo.Email ?? string.Empty
                    };

                    SetState(AuthState.Authenticated);
                    OnUserChanged?.Invoke(_currentUser);

                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, userInfo.Id ?? string.Empty),
                        new(ClaimTypes.Name, userInfo.Name ?? string.Empty),
                        new(ClaimTypes.Email, userInfo.Email ?? string.Empty)
                    };

                    var identity = new ClaimsIdentity(claims, "ServerAuth");
                    return new AuthenticationState(new ClaimsPrincipal(identity));
                }
            }
        }
        catch (HttpRequestException)
        {
            // Server not reachable - treat as unauthenticated
        }

        _currentUser = null;
        SetState(AuthState.Unauthenticated);
        OnUserChanged?.Invoke(null);

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    public async Task<bool> CheckSessionAsync()
    {
        SetState(AuthState.Checking);

        var authState = await GetAuthenticationStateAsync();
        return authState.User.Identity?.IsAuthenticated ?? false;
    }

    public Task<AuthResult> LoginAsync()
    {
        // In hosted mode, login is handled by redirecting to the server
        // The actual redirect is done via NavigationManager in the component
        return Task.FromResult(new AuthResult
        {
            Success = true,
            Error = null
        });
    }

    public string GetLoginUrl(string? returnUrl = null)
    {
        var url = AuthLoginEndpoint;
        if (!string.IsNullOrEmpty(returnUrl))
        {
            url += $"?returnUrl={Uri.EscapeDataString(returnUrl)}";
        }
        return url;
    }

    public string GetLogoutUrl()
    {
        return AuthLogoutEndpoint;
    }

    public Task<AuthResult> HandleCallbackAsync(string code, string state)
    {
        // In hosted mode, callback is handled by the server
        return Task.FromResult(new AuthResult { Success = true });
    }

    public async Task LogoutAsync()
    {
        _currentUser = null;
        SetState(AuthState.Unauthenticated);
        OnUserChanged?.Invoke(null);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public Task<string?> GetAccessTokenAsync()
    {
        // In hosted mode, tokens are managed by the server
        // Client doesn't need direct access to tokens
        return Task.FromResult<string?>(null);
    }

    public Task<string?> GetAccessTokenAsync(string[] scopes)
    {
        // In hosted mode, tokens are managed by the server
        return Task.FromResult<string?>(null);
    }

    public async Task<bool> IsTokenValidAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(AuthStatusEndpoint);
            if (response.IsSuccessStatusCode)
            {
                var status = await response.Content.ReadFromJsonAsync<AuthStatusDto>();
                return status?.IsAuthenticated ?? false;
            }
        }
        catch
        {
            // Server not reachable
        }

        return false;
    }

    public async Task<AuthResult> RefreshTokenAsync()
    {
        // In hosted mode, token refresh is automatic via cookies
        var isValid = await IsTokenValidAsync();
        return new AuthResult
        {
            Success = isValid,
            User = _currentUser
        };
    }

    public void NotifyStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private void SetState(AuthState newState)
    {
        if (_state != newState)
        {
            _state = newState;
            OnAuthStateChanged?.Invoke(newState);
        }
    }

    private class UserInfoDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public bool IsAuthenticated { get; set; }
    }

    private class AuthStatusDto
    {
        public bool IsAuthenticated { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
    }
}
