using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SelfOrganizer.Server.Services;

public class GoogleCalendarProxyService : IGoogleCalendarProxyService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleCalendarProxyService> _logger;

    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string AuthEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string CalendarApiBase = "https://www.googleapis.com/calendar/v3";

    public GoogleCalendarProxyService(
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<GoogleCalendarProxyService> logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;
    }

    public bool IsConfigured()
    {
        var clientId = _configuration["GoogleCalendar:ClientId"];
        var clientSecret = _configuration["GoogleCalendar:ClientSecret"];
        return !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret);
    }

    public GoogleAuthUrlResponse GenerateAuthUrl(string state)
    {
        try
        {
            var clientId = _configuration["GoogleCalendar:ClientId"];
            var redirectUri = _configuration["GoogleCalendar:RedirectUri"];
            var scopes = _configuration.GetSection("GoogleCalendar:Scopes").Get<string[]>();

            if (string.IsNullOrEmpty(clientId))
            {
                return new GoogleAuthUrlResponse
                {
                    Success = false,
                    Error = "Google Calendar Client ID not configured"
                };
            }

            // Generate PKCE code verifier and challenge
            var codeVerifier = GenerateCodeVerifier();
            var codeChallenge = GenerateCodeChallenge(codeVerifier);

            var scopeString = string.Join(" ", scopes ?? new[] { "https://www.googleapis.com/auth/calendar.readonly" });

            var authUrl = $"{AuthEndpoint}?" +
                $"client_id={Uri.EscapeDataString(clientId)}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri ?? "")}" +
                $"&response_type=code" +
                $"&scope={Uri.EscapeDataString(scopeString)}" +
                $"&access_type=offline" +
                $"&prompt=consent" +
                $"&state={Uri.EscapeDataString(state)}" +
                $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
                $"&code_challenge_method=S256";

            return new GoogleAuthUrlResponse
            {
                Success = true,
                AuthUrl = authUrl,
                CodeVerifier = codeVerifier,
                State = state
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating auth URL");
            return new GoogleAuthUrlResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<GoogleTokenResponse> ExchangeCodeAsync(string code, string codeVerifier)
    {
        try
        {
            var clientId = _configuration["GoogleCalendar:ClientId"];
            var clientSecret = _configuration["GoogleCalendar:ClientSecret"];
            var redirectUri = _configuration["GoogleCalendar:RedirectUri"];

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = clientId ?? "",
                ["client_secret"] = clientSecret ?? "",
                ["redirect_uri"] = redirectUri ?? "",
                ["grant_type"] = "authorization_code",
                ["code_verifier"] = codeVerifier
            });

            var response = await _httpClient.PostAsync(TokenEndpoint, content);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Token exchange failed: {Response}", json);
                return new GoogleTokenResponse
                {
                    Success = false,
                    Error = $"Token exchange failed: {response.StatusCode}"
                };
            }

            var tokenData = JsonSerializer.Deserialize<JsonElement>(json);

            return new GoogleTokenResponse
            {
                Success = true,
                AccessToken = tokenData.GetProperty("access_token").GetString(),
                RefreshToken = tokenData.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null,
                ExpiresIn = tokenData.GetProperty("expires_in").GetInt32(),
                TokenType = tokenData.GetProperty("token_type").GetString(),
                Scope = tokenData.TryGetProperty("scope", out var scope) ? scope.GetString() : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging code for tokens");
            return new GoogleTokenResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<GoogleTokenResponse> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var clientId = _configuration["GoogleCalendar:ClientId"];
            var clientSecret = _configuration["GoogleCalendar:ClientSecret"];

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["refresh_token"] = refreshToken,
                ["client_id"] = clientId ?? "",
                ["client_secret"] = clientSecret ?? "",
                ["grant_type"] = "refresh_token"
            });

            var response = await _httpClient.PostAsync(TokenEndpoint, content);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Token refresh failed: {Response}", json);
                return new GoogleTokenResponse
                {
                    Success = false,
                    Error = $"Token refresh failed: {response.StatusCode}"
                };
            }

            var tokenData = JsonSerializer.Deserialize<JsonElement>(json);

            return new GoogleTokenResponse
            {
                Success = true,
                AccessToken = tokenData.GetProperty("access_token").GetString(),
                ExpiresIn = tokenData.GetProperty("expires_in").GetInt32(),
                TokenType = tokenData.GetProperty("token_type").GetString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return new GoogleTokenResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<GoogleCalendarListResponse> GetCalendarsAsync(string accessToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{CalendarApiBase}/users/me/calendarList");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get calendars: {Response}", json);
                return new GoogleCalendarListResponse
                {
                    Success = false,
                    Error = $"Failed to get calendars: {response.StatusCode}"
                };
            }

            var data = JsonSerializer.Deserialize<JsonElement>(json);
            var calendars = new List<GoogleCalendarInfo>();

            if (data.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    calendars.Add(new GoogleCalendarInfo
                    {
                        Id = item.GetProperty("id").GetString() ?? "",
                        Summary = item.GetProperty("summary").GetString() ?? "",
                        Description = item.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                        BackgroundColor = item.TryGetProperty("backgroundColor", out var bg) ? bg.GetString() : null,
                        Primary = item.TryGetProperty("primary", out var primary) && primary.GetBoolean(),
                        AccessRole = item.TryGetProperty("accessRole", out var role) ? role.GetString() : null
                    });
                }
            }

            return new GoogleCalendarListResponse
            {
                Success = true,
                Calendars = calendars
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting calendars");
            return new GoogleCalendarListResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<GoogleEventsResponse> GetEventsAsync(string accessToken, string calendarId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var timeMin = startDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            var timeMax = endDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

            var url = $"{CalendarApiBase}/calendars/{Uri.EscapeDataString(calendarId)}/events" +
                $"?timeMin={Uri.EscapeDataString(timeMin)}" +
                $"&timeMax={Uri.EscapeDataString(timeMax)}" +
                $"&singleEvents=true" +
                $"&orderBy=startTime" +
                $"&maxResults=250";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get events: {Response}", json);
                return new GoogleEventsResponse
                {
                    Success = false,
                    Error = $"Failed to get events: {response.StatusCode}"
                };
            }

            var data = JsonSerializer.Deserialize<JsonElement>(json);
            var events = new List<GoogleEventInfo>();

            if (data.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var eventInfo = new GoogleEventInfo
                    {
                        Id = item.GetProperty("id").GetString() ?? "",
                        Summary = item.TryGetProperty("summary", out var summary) ? summary.GetString() ?? "(No title)" : "(No title)",
                        Description = item.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                        Location = item.TryGetProperty("location", out var loc) ? loc.GetString() : null,
                        Status = item.TryGetProperty("status", out var status) ? status.GetString() : null,
                        HtmlLink = item.TryGetProperty("htmlLink", out var link) ? link.GetString() : null,
                        RecurringEventId = item.TryGetProperty("recurringEventId", out var recId) ? recId.GetString() : null
                    };

                    // Parse start time
                    if (item.TryGetProperty("start", out var start))
                    {
                        if (start.TryGetProperty("dateTime", out var startDateTime))
                        {
                            eventInfo.Start = DateTime.Parse(startDateTime.GetString() ?? "");
                            eventInfo.AllDay = false;
                        }
                        else if (start.TryGetProperty("date", out var startDate2))
                        {
                            eventInfo.Start = DateTime.Parse(startDate2.GetString() ?? "");
                            eventInfo.AllDay = true;
                        }
                    }

                    // Parse end time
                    if (item.TryGetProperty("end", out var end))
                    {
                        if (end.TryGetProperty("dateTime", out var endDateTime))
                        {
                            eventInfo.End = DateTime.Parse(endDateTime.GetString() ?? "");
                        }
                        else if (end.TryGetProperty("date", out var endDate2))
                        {
                            eventInfo.End = DateTime.Parse(endDate2.GetString() ?? "");
                        }
                    }

                    // Parse attendees
                    if (item.TryGetProperty("attendees", out var attendees))
                    {
                        foreach (var attendee in attendees.EnumerateArray())
                        {
                            if (attendee.TryGetProperty("email", out var email))
                            {
                                eventInfo.Attendees.Add(email.GetString() ?? "");
                            }
                        }
                    }

                    // Parse organizer
                    if (item.TryGetProperty("organizer", out var organizer) &&
                        organizer.TryGetProperty("email", out var orgEmail))
                    {
                        eventInfo.Organizer = orgEmail.GetString();
                    }

                    events.Add(eventInfo);
                }
            }

            return new GoogleEventsResponse
            {
                Success = true,
                Events = events,
                NextPageToken = data.TryGetProperty("nextPageToken", out var token) ? token.GetString() : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting events");
            return new GoogleEventsResponse
            {
                Success = false,
                Error = ex.Message
            };
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
        var bytes = sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
