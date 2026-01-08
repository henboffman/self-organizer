using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.JSInterop;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Intelligence;

/// <summary>
/// Service for communicating with Ollama LLM API
/// </summary>
public class LlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private const string SettingsKey = "llm_settings";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public LlmService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Checks if the Ollama service is available
    /// </summary>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var settings = await GetSettingsAsync();
            if (!settings.IsEnabled)
                return false;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _httpClient.GetAsync($"{settings.Endpoint}/api/tags", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the current connection status including available models
    /// </summary>
    public async Task<LlmConnectionStatus> GetConnectionStatusAsync()
    {
        var status = new LlmConnectionStatus();
        var settings = await GetSettingsAsync();
        status.Endpoint = settings.Endpoint;
        status.Model = settings.Model;

        try
        {
            if (!settings.IsEnabled)
            {
                status.IsConnected = false;
                status.ErrorMessage = "LLM integration is disabled";
                return status;
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _httpClient.GetAsync($"{settings.Endpoint}/api/tags", cts.Token);

            if (response.IsSuccessStatusCode)
            {
                status.IsConnected = true;
                var tagsResponse = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(JsonOptions, cts.Token);
                status.AvailableModels = tagsResponse?.Models.Select(m => m.Name).ToList() ?? new List<string>();
            }
            else
            {
                status.IsConnected = false;
                status.ErrorMessage = $"Server returned {response.StatusCode}";
            }
        }
        catch (TaskCanceledException)
        {
            status.IsConnected = false;
            status.ErrorMessage = "Connection timed out";
        }
        catch (HttpRequestException ex)
        {
            status.IsConnected = false;
            status.ErrorMessage = $"Connection failed: {ex.Message}";
        }
        catch (Exception ex)
        {
            status.IsConnected = false;
            status.ErrorMessage = $"Error: {ex.Message}";
        }

        return status;
    }

    /// <summary>
    /// Generates text using the configured Ollama model
    /// </summary>
    public async Task<string> GenerateAsync(string prompt, LlmOptions? options = null)
    {
        var settings = await GetSettingsAsync();

        if (!settings.IsEnabled)
            throw new InvalidOperationException("LLM integration is disabled");

        var request = new OllamaGenerateRequest
        {
            Model = options?.Model ?? settings.Model,
            Prompt = prompt,
            System = options?.SystemPrompt,
            Stream = false,
            Options = new OllamaOptions
            {
                Temperature = options?.Temperature ?? 0.7,
                NumPredict = options?.MaxTokens ?? 2048
            }
        };

        var timeout = TimeSpan.FromSeconds(options?.TimeoutSeconds ?? settings.TimeoutSeconds);
        using var cts = new CancellationTokenSource(timeout);

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{settings.Endpoint}/api/generate",
                request,
                JsonOptions,
                cts.Token);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(JsonOptions, cts.Token);
            return result?.Response ?? string.Empty;
        }
        catch (TaskCanceledException)
        {
            throw new TimeoutException($"LLM request timed out after {timeout.TotalSeconds} seconds");
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed to communicate with Ollama: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Generates structured output by parsing JSON from the LLM response
    /// </summary>
    public async Task<T?> GenerateStructuredAsync<T>(string prompt, LlmOptions? options = null) where T : class
    {
        // Enhance prompt to request JSON output
        var structuredPrompt = $@"{prompt}

IMPORTANT: Respond with valid JSON only. Do not include any text before or after the JSON.
The response must be parseable JSON that matches this structure: {typeof(T).Name}";

        var response = await GenerateAsync(structuredPrompt, options);

        // Extract JSON from response (handle markdown code blocks)
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
            // Throw with details about what went wrong
            throw new InvalidOperationException($"Failed to parse LLM response as JSON: {ex.Message}. Response was: {json.Substring(0, Math.Min(500, json.Length))}...", ex);
        }
    }

    /// <summary>
    /// Gets the current LLM settings from localStorage
    /// </summary>
    public async Task<LlmSettings> GetSettingsAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", SettingsKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var settings = JsonSerializer.Deserialize<LlmSettings>(json);
                if (settings != null)
                    return settings;
            }
        }
        catch
        {
            // Ignore errors reading from localStorage
        }

        return new LlmSettings();
    }

    /// <summary>
    /// Saves LLM settings to localStorage
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
    /// Gets the list of available models from Ollama
    /// </summary>
    public async Task<List<string>> GetAvailableModelsAsync()
    {
        try
        {
            var settings = await GetSettingsAsync();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _httpClient.GetAsync($"{settings.Endpoint}/api/tags", cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var tagsResponse = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(JsonOptions, cts.Token);
                return tagsResponse?.Models.Select(m => m.Name).ToList() ?? new List<string>();
            }
        }
        catch
        {
            // Return empty list on error
        }

        return new List<string>();
    }

    /// <summary>
    /// Extracts JSON from an LLM response, handling markdown code blocks
    /// </summary>
    private static string ExtractJsonFromResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return string.Empty;

        // Try to extract JSON from markdown code blocks
        var codeBlockMatch = Regex.Match(response, @"```(?:json)?\s*([\s\S]*?)```", RegexOptions.IgnoreCase);
        if (codeBlockMatch.Success)
        {
            return codeBlockMatch.Groups[1].Value.Trim();
        }

        // Try to find JSON object or array
        var jsonMatch = Regex.Match(response, @"(\{[\s\S]*\}|\[[\s\S]*\])");
        if (jsonMatch.Success)
        {
            return jsonMatch.Groups[1].Value.Trim();
        }

        // Return trimmed response as-is
        return response.Trim();
    }
}
