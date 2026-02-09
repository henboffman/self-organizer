using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.JSInterop;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Intelligence;

/// <summary>
/// Service for communicating with LLM APIs via the server proxy.
/// All LLM requests are routed through the server to avoid CORS issues
/// and keep API keys secure on the server side.
/// </summary>
public class LlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private const string SettingsKey = "llm_settings";

    public LlmService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Checks if the LLM service is available
    /// </summary>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var settings = await GetSettingsAsync();
            if (!settings.IsEnabled)
                return false;

            var status = await GetConnectionStatusAsync();
            return status.IsConnected;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the current connection status from the server
    /// </summary>
    public async Task<LlmConnectionStatus> GetConnectionStatusAsync()
    {
        try
        {
            var settings = await GetSettingsAsync();
            var response = await _httpClient.GetAsync($"api/llm/status/{(int)settings.Provider}");

            if (response.IsSuccessStatusCode)
            {
                var status = await response.Content.ReadFromJsonAsync<LlmConnectionStatus>();
                return status ?? new LlmConnectionStatus { IsConnected = false, ErrorMessage = "Invalid response" };
            }

            return new LlmConnectionStatus { IsConnected = false, ErrorMessage = $"Server returned {response.StatusCode}" };
        }
        catch (Exception ex)
        {
            return new LlmConnectionStatus { IsConnected = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Generates text using the configured LLM via server proxy
    /// </summary>
    public async Task<string> GenerateAsync(string prompt, LlmOptions? options = null)
    {
        var settings = await GetSettingsAsync();

        if (!settings.IsEnabled)
            throw new InvalidOperationException("LLM integration is disabled");

        var request = new LlmProxyRequest
        {
            Provider = settings.Provider,
            Prompt = prompt,
            SystemPrompt = options?.SystemPrompt,
            Temperature = options?.Temperature ?? 1,
            MaxTokens = options?.MaxTokens ?? 2048
        };

        var response = await _httpClient.PostAsJsonAsync("api/llm/generate", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"LLM request failed: {response.StatusCode} - {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<LlmProxyResponse>();

        if (result == null || !result.Success)
            throw new InvalidOperationException(result?.ErrorMessage ?? "LLM request failed");

        return result.Response;
    }

    /// <summary>
    /// Generates structured output by parsing JSON from the LLM response
    /// </summary>
    public async Task<T?> GenerateStructuredAsync<T>(string prompt, LlmOptions? options = null) where T : class
    {
        var structuredPrompt = $@"{prompt}

IMPORTANT: Respond with valid JSON only. Do not include any text before or after the JSON.
The response must be parseable JSON that matches this structure: {typeof(T).Name}";

        var response = await GenerateAsync(structuredPrompt, options);
        var json = ExtractJsonFromResponse(response);

        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("LLM returned empty response");

        try
        {
            var result = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
                throw new InvalidOperationException("JSON deserialized to null");

            return result;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse LLM response as JSON: {ex.Message}. Response was: {json.Substring(0, Math.Min(500, json.Length))}...", ex);
        }
    }

    /// <summary>
    /// Gets the current LLM settings (provider selection from localStorage)
    /// </summary>
    public async Task<LlmSettings> GetSettingsAsync()
    {
        var settings = new LlmSettings();

        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", SettingsKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var userPrefs = JsonSerializer.Deserialize<LlmSettings>(json);
                if (userPrefs != null)
                {
                    settings = userPrefs;
                }
            }
        }
        catch
        {
            // Ignore errors reading from localStorage
        }

        return settings;
    }

    /// <summary>
    /// Saves LLM settings to localStorage (provider selection only)
    /// </summary>
    public async Task SaveSettingsAsync(LlmSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", SettingsKey, json);
        }
        catch
        {
            // Ignore errors writing to localStorage
        }
    }

    /// <summary>
    /// Gets LLM configuration from the server (synchronous wrapper)
    /// </summary>
    public LlmSettingsFromConfig GetConfigSettings()
    {
        // For synchronous calls, return defaults
        // Use GetConfigSettingsAsync for actual values
        return new LlmSettingsFromConfig();
    }

    /// <summary>
    /// Gets LLM configuration from the server asynchronously
    /// </summary>
    public async Task<LlmSettingsFromConfig> GetConfigSettingsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/llm/config");
            if (response.IsSuccessStatusCode)
            {
                var config = await response.Content.ReadFromJsonAsync<LlmSettingsFromConfig>();
                return config ?? new LlmSettingsFromConfig();
            }
        }
        catch
        {
            // Ignore errors
        }

        return new LlmSettingsFromConfig();
    }

    /// <summary>
    /// Gets the list of available models
    /// </summary>
    public async Task<List<string>> GetAvailableModelsAsync()
    {
        var status = await GetConnectionStatusAsync();
        return status.AvailableModels;
    }

    private static string ExtractJsonFromResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return string.Empty;

        var codeBlockMatch = Regex.Match(response, @"```(?:json)?\s*([\s\S]*?)```", RegexOptions.IgnoreCase);
        string json;
        if (codeBlockMatch.Success)
        {
            json = codeBlockMatch.Groups[1].Value.Trim();
        }
        else
        {
            var jsonMatch = Regex.Match(response, @"(\{[\s\S]*\}|\[[\s\S]*\])");
            json = jsonMatch.Success ? jsonMatch.Groups[1].Value.Trim() : response.Trim();
        }

        // Sanitize invalid escape sequences that LLMs sometimes generate
        return SanitizeJsonString(json);
    }

    /// <summary>
    /// Sanitizes JSON strings by fixing common LLM-generated escape sequence errors.
    /// LLMs sometimes generate invalid escape sequences like \& or \' which are not valid JSON.
    /// </summary>
    private static string SanitizeJsonString(string json)
    {
        if (string.IsNullOrEmpty(json))
            return json;

        // Fix invalid escape sequences: \& \' \` \# \@ \! \? \( \) \[ \] \{ \} \< \> \= \+ \* \% \$ \^
        // These are NOT valid JSON escape sequences but LLMs sometimes generate them
        // Valid JSON escapes are: \" \\ \/ \b \f \n \r \t \uXXXX
        var result = Regex.Replace(json, @"\\([^""\\\/bfnrtu])", match =>
        {
            var escapedChar = match.Groups[1].Value;
            // If it's a unicode escape like \u0000, keep it
            if (escapedChar.Length > 0 && char.IsDigit(escapedChar[0]))
                return match.Value;
            // Otherwise, just return the character without the backslash
            return escapedChar;
        });

        return result;
    }
}

// Request/Response DTOs for the proxy API
public class LlmProxyRequest
{
    public LlmProvider Provider { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string? SystemPrompt { get; set; }
    public double Temperature { get; set; } = 1;
    public int MaxTokens { get; set; } = 2048;
}

public class LlmProxyResponse
{
    public bool Success { get; set; }
    public string Response { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
