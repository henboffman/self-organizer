using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Intelligence;

/// <summary>
/// Service for communicating with various LLM APIs (Ollama, OpenAI, Azure OpenAI, Anthropic)
/// </summary>
public class LlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly IConfiguration _configuration;
    private const string SettingsKey = "llm_settings";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions CamelCaseJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public LlmService(HttpClient httpClient, IJSRuntime jsRuntime, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _configuration = configuration;
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

            return settings.Provider switch
            {
                LlmProvider.Ollama => await CheckOllamaAvailabilityAsync(settings),
                LlmProvider.OpenAI => !string.IsNullOrWhiteSpace(settings.OpenAIApiKey),
                LlmProvider.AzureOpenAI => CheckAzureOpenAIAvailability(settings),
                LlmProvider.Anthropic => !string.IsNullOrWhiteSpace(settings.AnthropicApiKey),
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckOllamaAvailabilityAsync(LlmSettings settings)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _httpClient.GetAsync($"{settings.OllamaEndpoint}/api/tags", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private bool CheckAzureOpenAIAvailability(LlmSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.AzureEndpoint) ||
            string.IsNullOrWhiteSpace(settings.AzureDeploymentName))
            return false;

        // If using API key auth, just check if key is present
        if (!settings.UseAzureADAuth)
            return !string.IsNullOrWhiteSpace(settings.AzureApiKey);

        // For Azure AD auth, check if credentials are present
        return !string.IsNullOrWhiteSpace(settings.AzureTenantId) &&
               !string.IsNullOrWhiteSpace(settings.AzureClientId) &&
               !string.IsNullOrWhiteSpace(settings.AzureClientSecret);
    }

    /// <summary>
    /// Gets the current connection status including available models
    /// </summary>
    public async Task<LlmConnectionStatus> GetConnectionStatusAsync()
    {
        var status = new LlmConnectionStatus();
        var settings = await GetSettingsAsync();

        try
        {
            if (!settings.IsEnabled)
            {
                status.IsConnected = false;
                status.ErrorMessage = "LLM integration is disabled";
                return status;
            }

            return settings.Provider switch
            {
                LlmProvider.Ollama => await GetOllamaConnectionStatusAsync(settings),
                LlmProvider.OpenAI => GetOpenAIConnectionStatus(settings),
                LlmProvider.AzureOpenAI => GetAzureOpenAIConnectionStatus(settings),
                LlmProvider.Anthropic => GetAnthropicConnectionStatus(settings),
                _ => new LlmConnectionStatus { IsConnected = false, ErrorMessage = "Unknown provider" }
            };
        }
        catch (Exception ex)
        {
            status.IsConnected = false;
            status.ErrorMessage = $"Error: {ex.Message}";
            return status;
        }
    }

    private async Task<LlmConnectionStatus> GetOllamaConnectionStatusAsync(LlmSettings settings)
    {
        var status = new LlmConnectionStatus
        {
            Endpoint = settings.OllamaEndpoint,
            Model = settings.OllamaModel
        };

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _httpClient.GetAsync($"{settings.OllamaEndpoint}/api/tags", cts.Token);

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

        return status;
    }

    private LlmConnectionStatus GetOpenAIConnectionStatus(LlmSettings settings)
    {
        var hasKey = !string.IsNullOrWhiteSpace(settings.OpenAIApiKey);
        return new LlmConnectionStatus
        {
            IsConnected = hasKey,
            Endpoint = settings.OpenAIEndpoint,
            Model = settings.OpenAIModel,
            ErrorMessage = hasKey ? null : "API key not configured",
            AvailableModels = hasKey ? new List<string>
            {
                "gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "gpt-4", "gpt-3.5-turbo"
            } : new List<string>()
        };
    }

    private LlmConnectionStatus GetAzureOpenAIConnectionStatus(LlmSettings settings)
    {
        var isConfigured = !string.IsNullOrWhiteSpace(settings.AzureEndpoint) &&
                          !string.IsNullOrWhiteSpace(settings.AzureDeploymentName) &&
                          ((!settings.UseAzureADAuth && !string.IsNullOrWhiteSpace(settings.AzureApiKey)) ||
                           (settings.UseAzureADAuth &&
                            !string.IsNullOrWhiteSpace(settings.AzureTenantId) &&
                            !string.IsNullOrWhiteSpace(settings.AzureClientId) &&
                            !string.IsNullOrWhiteSpace(settings.AzureClientSecret)));

        return new LlmConnectionStatus
        {
            IsConnected = isConfigured,
            Endpoint = settings.AzureEndpoint,
            Model = settings.AzureDeploymentName,
            ErrorMessage = isConfigured ? null : "Azure OpenAI not fully configured",
            AvailableModels = isConfigured ? new List<string> { settings.AzureDeploymentName } : new List<string>()
        };
    }

    private LlmConnectionStatus GetAnthropicConnectionStatus(LlmSettings settings)
    {
        var hasKey = !string.IsNullOrWhiteSpace(settings.AnthropicApiKey);
        return new LlmConnectionStatus
        {
            IsConnected = hasKey,
            Endpoint = settings.AnthropicEndpoint,
            Model = settings.AnthropicModel,
            ErrorMessage = hasKey ? null : "API key not configured",
            AvailableModels = hasKey ? new List<string>
            {
                "claude-3-5-sonnet-20241022", "claude-3-5-haiku-20241022",
                "claude-3-opus-20240229", "claude-3-sonnet-20240229", "claude-3-haiku-20240307"
            } : new List<string>()
        };
    }

    /// <summary>
    /// Generates text using the configured LLM
    /// </summary>
    public async Task<string> GenerateAsync(string prompt, LlmOptions? options = null)
    {
        var settings = await GetSettingsAsync();

        if (!settings.IsEnabled)
            throw new InvalidOperationException("LLM integration is disabled");

        return settings.Provider switch
        {
            LlmProvider.Ollama => await GenerateWithOllamaAsync(settings, prompt, options),
            LlmProvider.OpenAI => await GenerateWithOpenAIAsync(settings, prompt, options),
            LlmProvider.AzureOpenAI => await GenerateWithAzureOpenAIAsync(settings, prompt, options),
            LlmProvider.Anthropic => await GenerateWithAnthropicAsync(settings, prompt, options),
            _ => throw new InvalidOperationException($"Unknown provider: {settings.Provider}")
        };
    }

    private async Task<string> GenerateWithOllamaAsync(LlmSettings settings, string prompt, LlmOptions? options)
    {
        var request = new OllamaGenerateRequest
        {
            Model = options?.Model ?? settings.OllamaModel,
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
                $"{settings.OllamaEndpoint}/api/generate",
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

    private async Task<string> GenerateWithOpenAIAsync(LlmSettings settings, string prompt, LlmOptions? options)
    {
        var messages = new List<OpenAIChatMessage>();

        if (!string.IsNullOrWhiteSpace(options?.SystemPrompt))
        {
            messages.Add(new OpenAIChatMessage { Role = "system", Content = options.SystemPrompt });
        }

        messages.Add(new OpenAIChatMessage { Role = "user", Content = prompt });

        var request = new OpenAIChatRequest
        {
            Model = options?.Model ?? settings.OpenAIModel,
            Messages = messages,
            Temperature = options?.Temperature ?? 0.7,
            MaxTokens = options?.MaxTokens ?? 2048
        };

        var timeout = TimeSpan.FromSeconds(options?.TimeoutSeconds ?? settings.TimeoutSeconds);
        using var cts = new CancellationTokenSource(timeout);

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{settings.OpenAIEndpoint}/chat/completions");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.OpenAIApiKey);
            httpRequest.Content = JsonContent.Create(request, options: CamelCaseJsonOptions);

            var response = await _httpClient.SendAsync(httpRequest, cts.Token);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>(CamelCaseJsonOptions, cts.Token);
            return result?.Choices.FirstOrDefault()?.Message.Content ?? string.Empty;
        }
        catch (TaskCanceledException)
        {
            throw new TimeoutException($"LLM request timed out after {timeout.TotalSeconds} seconds");
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed to communicate with OpenAI: {ex.Message}", ex);
        }
    }

    private async Task<string> GenerateWithAzureOpenAIAsync(LlmSettings settings, string prompt, LlmOptions? options)
    {
        var messages = new List<OpenAIChatMessage>();

        if (!string.IsNullOrWhiteSpace(options?.SystemPrompt))
        {
            messages.Add(new OpenAIChatMessage { Role = "system", Content = options.SystemPrompt });
        }

        messages.Add(new OpenAIChatMessage { Role = "user", Content = prompt });

        var request = new OpenAIChatRequest
        {
            Messages = messages,
            Temperature = options?.Temperature ?? 0.7,
            MaxTokens = options?.MaxTokens ?? 2048
        };

        var endpoint = $"{settings.AzureEndpoint.TrimEnd('/')}/openai/deployments/{settings.AzureDeploymentName}/chat/completions?api-version={settings.AzureApiVersion}";

        var timeout = TimeSpan.FromSeconds(options?.TimeoutSeconds ?? settings.TimeoutSeconds);
        using var cts = new CancellationTokenSource(timeout);

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);

            if (settings.UseAzureADAuth)
            {
                // Note: In a WebAssembly context, Azure AD token acquisition needs to be handled
                // through MSAL.js or a backend service. For now, we'll throw a descriptive error.
                throw new InvalidOperationException(
                    "Azure AD authentication is not yet supported in browser context. Please use API key authentication.");
            }
            else
            {
                httpRequest.Headers.Add("api-key", settings.AzureApiKey);
            }

            httpRequest.Content = JsonContent.Create(request, options: CamelCaseJsonOptions);

            var response = await _httpClient.SendAsync(httpRequest, cts.Token);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>(CamelCaseJsonOptions, cts.Token);
            return result?.Choices.FirstOrDefault()?.Message.Content ?? string.Empty;
        }
        catch (TaskCanceledException)
        {
            throw new TimeoutException($"LLM request timed out after {timeout.TotalSeconds} seconds");
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed to communicate with Azure OpenAI: {ex.Message}", ex);
        }
    }

    private async Task<string> GenerateWithAnthropicAsync(LlmSettings settings, string prompt, LlmOptions? options)
    {
        var request = new AnthropicMessageRequest
        {
            Model = options?.Model ?? settings.AnthropicModel,
            Messages = new List<AnthropicMessage>
            {
                new AnthropicMessage { Role = "user", Content = prompt }
            },
            System = options?.SystemPrompt,
            MaxTokens = options?.MaxTokens ?? 2048,
            Temperature = options?.Temperature ?? 0.7
        };

        var timeout = TimeSpan.FromSeconds(options?.TimeoutSeconds ?? settings.TimeoutSeconds);
        using var cts = new CancellationTokenSource(timeout);

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{settings.AnthropicEndpoint}/v1/messages");
            httpRequest.Headers.Add("x-api-key", settings.AnthropicApiKey);
            httpRequest.Headers.Add("anthropic-version", "2023-06-01");
            httpRequest.Content = JsonContent.Create(request, options: JsonOptions);

            var response = await _httpClient.SendAsync(httpRequest, cts.Token);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AnthropicMessageResponse>(JsonOptions, cts.Token);
            return result?.Content.FirstOrDefault()?.Text ?? string.Empty;
        }
        catch (TaskCanceledException)
        {
            throw new TimeoutException($"LLM request timed out after {timeout.TotalSeconds} seconds");
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed to communicate with Anthropic: {ex.Message}", ex);
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
    /// Gets the current LLM settings, merging user preferences with configuration.
    /// User preferences control provider selection; API keys come from appsettings.
    /// </summary>
    public async Task<LlmSettings> GetSettingsAsync()
    {
        // Start with defaults
        var settings = new LlmSettings();

        // Load user preferences from localStorage (provider selection, non-sensitive settings)
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

        // Override API keys and sensitive settings from configuration (appsettings.json / appsettings.Local.json)
        var llmConfig = _configuration.GetSection("LlmSettings");

        // Ollama settings from config
        var ollamaEndpoint = llmConfig.GetValue<string>("Ollama:Endpoint");
        var ollamaModel = llmConfig.GetValue<string>("Ollama:Model");
        if (!string.IsNullOrWhiteSpace(ollamaEndpoint))
            settings.OllamaEndpoint = ollamaEndpoint;
        if (!string.IsNullOrWhiteSpace(ollamaModel))
            settings.OllamaModel = ollamaModel;

        // OpenAI settings from config (API key always from config)
        var openAiApiKey = llmConfig.GetValue<string>("OpenAI:ApiKey");
        var openAiEndpoint = llmConfig.GetValue<string>("OpenAI:Endpoint");
        var openAiModel = llmConfig.GetValue<string>("OpenAI:Model");
        if (!string.IsNullOrWhiteSpace(openAiApiKey))
            settings.OpenAIApiKey = openAiApiKey;
        if (!string.IsNullOrWhiteSpace(openAiEndpoint))
            settings.OpenAIEndpoint = openAiEndpoint;
        if (!string.IsNullOrWhiteSpace(openAiModel))
            settings.OpenAIModel = openAiModel;

        // Azure OpenAI settings from config (API key always from config)
        var azureApiKey = llmConfig.GetValue<string>("AzureOpenAI:ApiKey");
        var azureEndpoint = llmConfig.GetValue<string>("AzureOpenAI:Endpoint");
        var azureDeployment = llmConfig.GetValue<string>("AzureOpenAI:DeploymentName");
        var azureApiVersion = llmConfig.GetValue<string>("AzureOpenAI:ApiVersion");
        if (!string.IsNullOrWhiteSpace(azureApiKey))
            settings.AzureApiKey = azureApiKey;
        if (!string.IsNullOrWhiteSpace(azureEndpoint))
            settings.AzureEndpoint = azureEndpoint;
        if (!string.IsNullOrWhiteSpace(azureDeployment))
            settings.AzureDeploymentName = azureDeployment;
        if (!string.IsNullOrWhiteSpace(azureApiVersion))
            settings.AzureApiVersion = azureApiVersion;

        // Anthropic settings from config (API key always from config)
        var anthropicApiKey = llmConfig.GetValue<string>("Anthropic:ApiKey");
        var anthropicEndpoint = llmConfig.GetValue<string>("Anthropic:Endpoint");
        var anthropicModel = llmConfig.GetValue<string>("Anthropic:Model");
        if (!string.IsNullOrWhiteSpace(anthropicApiKey))
            settings.AnthropicApiKey = anthropicApiKey;
        if (!string.IsNullOrWhiteSpace(anthropicEndpoint))
            settings.AnthropicEndpoint = anthropicEndpoint;
        if (!string.IsNullOrWhiteSpace(anthropicModel))
            settings.AnthropicModel = anthropicModel;

        // Timeout from config (if specified)
        var timeout = llmConfig.GetValue<int?>("TimeoutSeconds");
        if (timeout.HasValue && timeout.Value > 0)
            settings.TimeoutSeconds = timeout.Value;

        return settings;
    }

    /// <summary>
    /// Gets LLM settings directly from configuration (without user preferences).
    /// Used by Settings UI to show what's configured in appsettings.
    /// </summary>
    public LlmSettingsFromConfig GetConfigSettings()
    {
        var llmConfig = _configuration.GetSection("LlmSettings");

        return new LlmSettingsFromConfig
        {
            // Ollama
            OllamaEndpoint = llmConfig.GetValue<string>("Ollama:Endpoint") ?? "http://localhost:11434",
            OllamaModel = llmConfig.GetValue<string>("Ollama:Model") ?? "llama3.2",

            // OpenAI
            OpenAIEndpoint = llmConfig.GetValue<string>("OpenAI:Endpoint") ?? "https://api.openai.com/v1",
            OpenAIModel = llmConfig.GetValue<string>("OpenAI:Model") ?? "gpt-4o",
            HasOpenAIApiKey = !string.IsNullOrWhiteSpace(llmConfig.GetValue<string>("OpenAI:ApiKey")),

            // Azure OpenAI
            AzureEndpoint = llmConfig.GetValue<string>("AzureOpenAI:Endpoint") ?? "",
            AzureDeploymentName = llmConfig.GetValue<string>("AzureOpenAI:DeploymentName") ?? "",
            AzureApiVersion = llmConfig.GetValue<string>("AzureOpenAI:ApiVersion") ?? "2024-02-01",
            HasAzureApiKey = !string.IsNullOrWhiteSpace(llmConfig.GetValue<string>("AzureOpenAI:ApiKey")),

            // Anthropic
            AnthropicEndpoint = llmConfig.GetValue<string>("Anthropic:Endpoint") ?? "https://api.anthropic.com",
            AnthropicModel = llmConfig.GetValue<string>("Anthropic:Model") ?? "claude-3-5-sonnet-20241022",
            HasAnthropicApiKey = !string.IsNullOrWhiteSpace(llmConfig.GetValue<string>("Anthropic:ApiKey")),

            TimeoutSeconds = llmConfig.GetValue<int?>("TimeoutSeconds") ?? 60
        };
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
    /// Gets the list of available models from the configured provider
    /// </summary>
    public async Task<List<string>> GetAvailableModelsAsync()
    {
        var settings = await GetSettingsAsync();
        var status = await GetConnectionStatusAsync();
        return status.AvailableModels;
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
