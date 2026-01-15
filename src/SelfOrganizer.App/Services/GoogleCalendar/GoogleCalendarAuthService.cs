using System.Net.Http.Json;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.GoogleCalendar;

public class GoogleCalendarAuthService : IGoogleCalendarAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IRepository<UserPreferences> _preferencesRepository;

    // Temporary storage for PKCE code verifier during OAuth flow
    private string? _pendingCodeVerifier;
    private string? _pendingState;

    public GoogleCalendarAuthService(
        HttpClient httpClient,
        IRepository<UserPreferences> preferencesRepository)
    {
        _httpClient = httpClient;
        _preferencesRepository = preferencesRepository;
    }

    public async Task<bool> IsConfiguredAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/google-calendar/status");
            if (response.IsSuccessStatusCode)
            {
                var status = await response.Content.ReadFromJsonAsync<GoogleCalendarStatusResponse>();
                return status?.IsConfigured ?? false;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<GoogleAuthFlowResult> StartAuthFlowAsync()
    {
        try
        {
            _pendingState = Guid.NewGuid().ToString();

            var response = await _httpClient.PostAsJsonAsync("/api/google-calendar/auth-url", new { State = _pendingState });

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new GoogleAuthFlowResult
                {
                    Success = false,
                    Error = $"Failed to generate auth URL: {response.StatusCode}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<GoogleAuthUrlServerResponse>();

            if (result == null || !result.Success)
            {
                return new GoogleAuthFlowResult
                {
                    Success = false,
                    Error = result?.Error ?? "Unknown error"
                };
            }

            // Store the code verifier for the completion step
            _pendingCodeVerifier = result.CodeVerifier;

            return new GoogleAuthFlowResult
            {
                Success = true,
                AuthUrl = result.AuthUrl
            };
        }
        catch (Exception ex)
        {
            return new GoogleAuthFlowResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<GoogleAuthResult> CompleteAuthFlowAsync(string code)
    {
        try
        {
            if (string.IsNullOrEmpty(_pendingCodeVerifier))
            {
                return new GoogleAuthResult
                {
                    Success = false,
                    Error = "No pending auth flow. Please start the authorization again."
                };
            }

            var response = await _httpClient.PostAsJsonAsync("/api/google-calendar/token", new
            {
                Code = code,
                CodeVerifier = _pendingCodeVerifier
            });

            if (!response.IsSuccessStatusCode)
            {
                return new GoogleAuthResult
                {
                    Success = false,
                    Error = $"Token exchange failed: {response.StatusCode}"
                };
            }

            var tokenResult = await response.Content.ReadFromJsonAsync<GoogleTokenServerResponse>();

            if (tokenResult == null || !tokenResult.Success)
            {
                return new GoogleAuthResult
                {
                    Success = false,
                    Error = tokenResult?.Error ?? "Token exchange failed"
                };
            }

            // Clear the pending verifier
            _pendingCodeVerifier = null;
            _pendingState = null;

            // Save the tokens to preferences
            var prefs = await GetOrCreatePreferencesAsync();
            prefs.GoogleCalendarAccessToken = tokenResult.AccessToken;
            prefs.GoogleCalendarRefreshToken = tokenResult.RefreshToken;
            prefs.GoogleCalendarTokenExpiry = DateTime.UtcNow.AddSeconds(tokenResult.ExpiresIn - 60); // Buffer of 60 seconds

            await _preferencesRepository.UpdateAsync(prefs);

            // Get user's email from calendars
            var email = await GetUserEmailAsync(tokenResult.AccessToken!);

            if (!string.IsNullOrEmpty(email))
            {
                prefs.GoogleCalendarEmail = email;
                await _preferencesRepository.UpdateAsync(prefs);
            }

            return new GoogleAuthResult
            {
                Success = true,
                Email = email
            };
        }
        catch (Exception ex)
        {
            return new GoogleAuthResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<string?> GetValidAccessTokenAsync()
    {
        var prefs = await GetPreferencesAsync();
        if (prefs == null)
            return null;

        // Check if we have a token and if it's still valid
        if (!string.IsNullOrEmpty(prefs.GoogleCalendarAccessToken))
        {
            if (prefs.GoogleCalendarTokenExpiry.HasValue &&
                prefs.GoogleCalendarTokenExpiry.Value > DateTime.UtcNow)
            {
                return prefs.GoogleCalendarAccessToken;
            }

            // Token expired, try to refresh
            if (!string.IsNullOrEmpty(prefs.GoogleCalendarRefreshToken))
            {
                var newToken = await RefreshTokenAsync(prefs.GoogleCalendarRefreshToken);
                if (!string.IsNullOrEmpty(newToken))
                {
                    return newToken;
                }
            }
        }

        return null;
    }

    public async Task<bool> IsConnectedAsync()
    {
        var prefs = await GetPreferencesAsync();
        return prefs != null &&
               !string.IsNullOrEmpty(prefs.GoogleCalendarRefreshToken);
    }

    public async Task DisconnectAsync()
    {
        var prefs = await GetPreferencesAsync();
        if (prefs != null)
        {
            prefs.GoogleCalendarAccessToken = null;
            prefs.GoogleCalendarRefreshToken = null;
            prefs.GoogleCalendarTokenExpiry = null;
            prefs.GoogleCalendarEmail = null;
            prefs.GoogleCalendarSelectedCalendarIds = null;
            prefs.GoogleCalendarLastSyncTime = null;

            await _preferencesRepository.UpdateAsync(prefs);
        }
    }

    private async Task<string?> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/google-calendar/refresh", new
            {
                RefreshToken = refreshToken
            });

            if (!response.IsSuccessStatusCode)
                return null;

            var result = await response.Content.ReadFromJsonAsync<GoogleTokenServerResponse>();

            if (result == null || !result.Success)
                return null;

            // Save the new token
            var prefs = await GetOrCreatePreferencesAsync();
            prefs.GoogleCalendarAccessToken = result.AccessToken;
            prefs.GoogleCalendarTokenExpiry = DateTime.UtcNow.AddSeconds(result.ExpiresIn - 60);
            await _preferencesRepository.UpdateAsync(prefs);

            return result.AccessToken;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> GetUserEmailAsync(string accessToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/google-calendar/calendars");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;

            var result = await response.Content.ReadFromJsonAsync<GoogleCalendarListServerResponse>();
            if (result == null || !result.Success)
                return null;

            // Find the primary calendar - its ID is usually the user's email
            var primary = result.Calendars?.FirstOrDefault(c => c.Primary);
            return primary?.Id;
        }
        catch
        {
            return null;
        }
    }

    private async Task<UserPreferences?> GetPreferencesAsync()
    {
        var prefs = await _preferencesRepository.GetAllAsync();
        return prefs.FirstOrDefault();
    }

    private async Task<UserPreferences> GetOrCreatePreferencesAsync()
    {
        var prefs = await GetPreferencesAsync();
        if (prefs == null)
        {
            prefs = new UserPreferences();
            prefs = await _preferencesRepository.AddAsync(prefs);
        }
        return prefs;
    }

    // Server response DTOs
    private class GoogleCalendarStatusResponse
    {
        public bool IsConfigured { get; set; }
    }

    private class GoogleAuthUrlServerResponse
    {
        public bool Success { get; set; }
        public string? AuthUrl { get; set; }
        public string? CodeVerifier { get; set; }
        public string? State { get; set; }
        public string? Error { get; set; }
    }

    private class GoogleTokenServerResponse
    {
        public bool Success { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string? Error { get; set; }
    }

    private class GoogleCalendarListServerResponse
    {
        public bool Success { get; set; }
        public List<GoogleCalendarServerInfo>? Calendars { get; set; }
        public string? Error { get; set; }
    }

    private class GoogleCalendarServerInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? BackgroundColor { get; set; }
        public bool Primary { get; set; }
    }
}
