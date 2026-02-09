using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.EntraAuth;

/// <summary>
/// Service for Microsoft Entra (Azure AD) authentication.
/// Implements OAuth 2.0 Authorization Code Flow with PKCE for Blazor WebAssembly.
/// Follows the statechart model from STATECHARTS.md for Entra Authentication Component.
/// </summary>
public class EntraAuthService : IEntraAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly IRepository<UserPreferences> _preferencesRepository;
    private readonly EntraAuthConfig _config;

    // PKCE flow state
    private string? _pendingCodeVerifier;
    private string? _pendingState;

    // Token storage
    private string? _accessToken;
    private string? _refreshToken;
    private DateTime? _tokenExpiry;
    private EntraUser? _currentUser;
    private AuthState _state = AuthState.Checking;

    private const string TokenStorageKey = "entra_tokens";

    public EntraAuthService(
        HttpClient httpClient,
        IJSRuntime jsRuntime,
        IRepository<UserPreferences> preferencesRepository,
        EntraAuthConfig config)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _preferencesRepository = preferencesRepository;
        _config = config;

        // Initialize state based on config
        _state = _config.Enabled ? AuthState.Checking : AuthState.Disabled;
    }

    public bool IsEnabled => _config.Enabled;

    public AuthState State
    {
        get => _state;
        private set
        {
            if (_state != value)
            {
                _state = value;
                OnAuthStateChanged?.Invoke(_state);
            }
        }
    }

    public EntraUser? CurrentUser
    {
        get => _currentUser;
        private set
        {
            if (_currentUser != value)
            {
                _currentUser = value;
                OnUserChanged?.Invoke(_currentUser);
            }
        }
    }

    public event Action<AuthState>? OnAuthStateChanged;
    public event Action<EntraUser?>? OnUserChanged;

    /// <summary>
    /// Checks for existing session on app load.
    /// Petri net transition: P1 (CheckingSession) → T1 → P2 (SessionFound) or P3 (NoSession)
    /// </summary>
    public async Task<bool> CheckSessionAsync()
    {
        if (!_config.Enabled)
        {
            State = AuthState.Disabled;
            return false;
        }

        State = AuthState.Checking;

        try
        {
            // Try to load tokens from storage
            var storedTokens = await LoadTokensFromStorageAsync();
            if (storedTokens == null)
            {
                State = AuthState.Unauthenticated;
                return false;
            }

            _accessToken = storedTokens.AccessToken;
            _refreshToken = storedTokens.RefreshToken;
            _tokenExpiry = storedTokens.ExpiresAt;

            // Check if token is still valid
            if (await IsTokenValidAsync())
            {
                // Fetch user info
                var user = await FetchUserInfoAsync();
                if (user != null)
                {
                    CurrentUser = user;
                    State = AuthState.Authenticated;
                    return true;
                }
            }

            // Try to refresh the token
            var refreshResult = await RefreshTokenAsync();
            if (refreshResult.Success)
            {
                State = AuthState.Authenticated;
                return true;
            }

            // Session is invalid
            State = AuthState.Unauthenticated;
            return false;
        }
        catch
        {
            State = AuthState.Unauthenticated;
            return false;
        }
    }

    /// <summary>
    /// Initiates the login flow (redirect to Entra).
    /// Petri net transition: P3 (Unauthenticated) → T2 (StartLogin) → P4 (RedirectToEntra)
    /// </summary>
    public async Task<AuthResult> LoginAsync()
    {
        if (!_config.Enabled)
        {
            return new AuthResult
            {
                Success = false,
                Error = "Entra authentication is not enabled."
            };
        }

        try
        {
            State = AuthState.Authenticating;

            // Generate PKCE code verifier and challenge
            _pendingCodeVerifier = GenerateCodeVerifier();
            var codeChallenge = GenerateCodeChallenge(_pendingCodeVerifier);
            _pendingState = Guid.NewGuid().ToString();

            // Build the authorization URL
            var scopes = string.Join(" ", _config.Scopes);
            var authUrl = $"https://login.microsoftonline.com/{_config.TenantId}/oauth2/v2.0/authorize?" +
                $"client_id={Uri.EscapeDataString(_config.ClientId)}" +
                $"&response_type=code" +
                $"&redirect_uri={Uri.EscapeDataString(_config.RedirectUri)}" +
                $"&scope={Uri.EscapeDataString(scopes)}" +
                $"&state={Uri.EscapeDataString(_pendingState)}" +
                $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
                $"&code_challenge_method=S256" +
                $"&response_mode=query";

            // Return the auth URL for navigation
            return new AuthResult
            {
                Success = true,
                AccessToken = authUrl // Using AccessToken field to pass the URL
            };
        }
        catch (Exception ex)
        {
            State = AuthState.Error;
            return new AuthResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Handles the OAuth callback after redirect.
    /// Petri net transition: P5 (EntraCallback) → T4 (ExchangeCode) → P6 (TokenReceived)
    /// </summary>
    public async Task<AuthResult> HandleCallbackAsync(string code, string state)
    {
        if (!_config.Enabled)
        {
            return new AuthResult
            {
                Success = false,
                Error = "Entra authentication is not enabled."
            };
        }

        try
        {
            // Validate state
            if (state != _pendingState)
            {
                State = AuthState.Error;
                return new AuthResult
                {
                    Success = false,
                    Error = "Invalid state parameter. Possible CSRF attack."
                };
            }

            if (string.IsNullOrEmpty(_pendingCodeVerifier))
            {
                State = AuthState.Error;
                return new AuthResult
                {
                    Success = false,
                    Error = "No pending auth flow. Please start the authorization again."
                };
            }

            State = AuthState.Authenticating;

            // Exchange code for tokens
            var tokenRequest = new Dictionary<string, string>
            {
                ["client_id"] = _config.ClientId,
                ["scope"] = string.Join(" ", _config.Scopes),
                ["code"] = code,
                ["redirect_uri"] = _config.RedirectUri,
                ["grant_type"] = "authorization_code",
                ["code_verifier"] = _pendingCodeVerifier
            };

            var tokenResponse = await _httpClient.PostAsync(
                $"https://login.microsoftonline.com/{_config.TenantId}/oauth2/v2.0/token",
                new FormUrlEncodedContent(tokenRequest));

            if (!tokenResponse.IsSuccessStatusCode)
            {
                var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                State = AuthState.Error;
                return new AuthResult
                {
                    Success = false,
                    Error = $"Token exchange failed: {tokenResponse.StatusCode}",
                    ErrorDescription = errorContent
                };
            }

            var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<EntraTokenResponse>();

            if (tokenResult == null)
            {
                State = AuthState.Error;
                return new AuthResult
                {
                    Success = false,
                    Error = "Failed to parse token response."
                };
            }

            // Store tokens
            _accessToken = tokenResult.AccessToken;
            _refreshToken = tokenResult.RefreshToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResult.ExpiresIn - 60); // 60 second buffer

            // Clear PKCE state
            _pendingCodeVerifier = null;
            _pendingState = null;

            // Save tokens to storage
            await SaveTokensToStorageAsync();

            // Fetch user info
            var user = await FetchUserInfoAsync();
            if (user != null)
            {
                CurrentUser = user;
            }

            State = AuthState.Authenticated;

            return new AuthResult
            {
                Success = true,
                User = user,
                AccessToken = _accessToken,
                ExpiresAt = _tokenExpiry
            };
        }
        catch (Exception ex)
        {
            State = AuthState.Error;
            return new AuthResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Logs the user out.
    /// Petri net transition: P7 (Authenticated) → T7 (Logout) → P3 (Unauthenticated)
    /// </summary>
    public async Task LogoutAsync()
    {
        _accessToken = null;
        _refreshToken = null;
        _tokenExpiry = null;
        CurrentUser = null;

        await ClearTokensFromStorageAsync();

        State = AuthState.Unauthenticated;
    }

    /// <summary>
    /// Gets the current access token for API calls.
    /// Will refresh if expired.
    /// </summary>
    public async Task<string?> GetAccessTokenAsync()
    {
        if (!_config.Enabled)
            return null;

        if (string.IsNullOrEmpty(_accessToken))
            return null;

        if (_tokenExpiry.HasValue && _tokenExpiry.Value > DateTime.UtcNow)
            return _accessToken;

        // Token expired, try to refresh
        var result = await RefreshTokenAsync();
        return result.Success ? _accessToken : null;
    }

    /// <summary>
    /// Gets a token for a specific scope (e.g., Microsoft Graph).
    /// </summary>
    public async Task<string?> GetAccessTokenAsync(string[] scopes)
    {
        if (!_config.Enabled || string.IsNullOrEmpty(_refreshToken))
            return null;

        try
        {
            State = AuthState.Refreshing;

            var tokenRequest = new Dictionary<string, string>
            {
                ["client_id"] = _config.ClientId,
                ["scope"] = string.Join(" ", scopes),
                ["refresh_token"] = _refreshToken,
                ["grant_type"] = "refresh_token"
            };

            var response = await _httpClient.PostAsync(
                $"https://login.microsoftonline.com/{_config.TenantId}/oauth2/v2.0/token",
                new FormUrlEncodedContent(tokenRequest));

            if (!response.IsSuccessStatusCode)
            {
                State = AuthState.Error;
                return null;
            }

            var tokenResult = await response.Content.ReadFromJsonAsync<EntraTokenResponse>();

            State = AuthState.Authenticated;
            return tokenResult?.AccessToken;
        }
        catch
        {
            State = AuthState.Error;
            return null;
        }
    }

    /// <summary>
    /// Checks if the current token is valid.
    /// </summary>
    public Task<bool> IsTokenValidAsync()
    {
        if (string.IsNullOrEmpty(_accessToken))
            return Task.FromResult(false);

        if (!_tokenExpiry.HasValue)
            return Task.FromResult(false);

        return Task.FromResult(_tokenExpiry.Value > DateTime.UtcNow);
    }

    /// <summary>
    /// Refreshes the access token.
    /// Petri net transition: P8 (TokenExpired) → T5 (RefreshToken) → P6 (TokenReceived) or P9 (RefreshFailed)
    /// </summary>
    public async Task<AuthResult> RefreshTokenAsync()
    {
        if (!_config.Enabled || string.IsNullOrEmpty(_refreshToken))
        {
            return new AuthResult
            {
                Success = false,
                Error = "No refresh token available."
            };
        }

        try
        {
            State = AuthState.Refreshing;

            var tokenRequest = new Dictionary<string, string>
            {
                ["client_id"] = _config.ClientId,
                ["scope"] = string.Join(" ", _config.Scopes),
                ["refresh_token"] = _refreshToken,
                ["grant_type"] = "refresh_token"
            };

            var response = await _httpClient.PostAsync(
                $"https://login.microsoftonline.com/{_config.TenantId}/oauth2/v2.0/token",
                new FormUrlEncodedContent(tokenRequest));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                State = AuthState.Unauthenticated;
                return new AuthResult
                {
                    Success = false,
                    Error = "Token refresh failed.",
                    ErrorDescription = errorContent
                };
            }

            var tokenResult = await response.Content.ReadFromJsonAsync<EntraTokenResponse>();

            if (tokenResult == null)
            {
                State = AuthState.Unauthenticated;
                return new AuthResult
                {
                    Success = false,
                    Error = "Failed to parse token response."
                };
            }

            _accessToken = tokenResult.AccessToken;
            if (!string.IsNullOrEmpty(tokenResult.RefreshToken))
            {
                _refreshToken = tokenResult.RefreshToken;
            }
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResult.ExpiresIn - 60);

            await SaveTokensToStorageAsync();

            State = AuthState.Authenticated;

            return new AuthResult
            {
                Success = true,
                AccessToken = _accessToken,
                ExpiresAt = _tokenExpiry
            };
        }
        catch (Exception ex)
        {
            State = AuthState.Error;
            return new AuthResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    #region Private Helper Methods

    private async Task<EntraUser?> FetchUserInfoAsync()
    {
        if (string.IsNullOrEmpty(_accessToken))
            return null;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;

            var userInfo = await response.Content.ReadFromJsonAsync<GraphUserResponse>();
            if (userInfo == null)
                return null;

            return new EntraUser
            {
                Id = userInfo.Id,
                DisplayName = userInfo.DisplayName ?? string.Empty,
                Email = userInfo.Mail ?? userInfo.UserPrincipalName ?? string.Empty,
                UserPrincipalName = userInfo.UserPrincipalName ?? string.Empty,
                GivenName = userInfo.GivenName,
                Surname = userInfo.Surname
            };
        }
        catch
        {
            return null;
        }
    }

    private async Task<StoredTokens?> LoadTokensFromStorageAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>(
                "localStorage.getItem",
                TokenStorageKey);

            if (string.IsNullOrEmpty(json))
                return null;

            return JsonSerializer.Deserialize<StoredTokens>(json);
        }
        catch
        {
            return null;
        }
    }

    private async Task SaveTokensToStorageAsync()
    {
        try
        {
            var tokens = new StoredTokens
            {
                AccessToken = _accessToken,
                RefreshToken = _refreshToken,
                ExpiresAt = _tokenExpiry
            };

            var json = JsonSerializer.Serialize(tokens);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenStorageKey, json);
        }
        catch
        {
            // Ignore storage errors
        }
    }

    private async Task ClearTokensFromStorageAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenStorageKey);
        }
        catch
        {
            // Ignore storage errors
        }
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(codeVerifier);
        var hash = sha256.ComputeHash(bytes);
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    #endregion

    #region Helper Classes

    private class StoredTokens
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    private class EntraTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string? Scope { get; set; }
        public string TokenType { get; set; } = string.Empty;
    }

    private class GraphUserResponse
    {
        public string Id { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? GivenName { get; set; }
        public string? Surname { get; set; }
        public string? Mail { get; set; }
        public string? UserPrincipalName { get; set; }
    }

    #endregion
}
