using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Identity;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.Server.Services;

public class LlmProxyService : ILlmProxyService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LlmProxyService> _logger;

    private static readonly JsonSerializerOptions SnakeCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public LlmProxyService(HttpClient httpClient, IConfiguration configuration, ILogger<LlmProxyService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LlmProxyResponse> GenerateAsync(LlmProxyRequest request)
    {
        try
        {
            return request.Provider switch
            {
                LlmProvider.Ollama => await GenerateWithOllamaAsync(request),
                LlmProvider.OpenAI => await GenerateWithOpenAIAsync(request),
                LlmProvider.AzureOpenAI => await GenerateWithAzureOpenAIAsync(request),
                LlmProvider.Anthropic => await GenerateWithAnthropicAsync(request),
                _ => new LlmProxyResponse { Success = false, ErrorMessage = $"Unknown provider: {request.Provider}" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating LLM response for provider {Provider}", request.Provider);
            return new LlmProxyResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<LlmConnectionStatus> GetConnectionStatusAsync(LlmProvider provider)
    {
        var config = GetConfigSettings();

        return provider switch
        {
            LlmProvider.Ollama => await GetOllamaStatusAsync(config),
            LlmProvider.OpenAI => GetOpenAIStatus(config),
            LlmProvider.AzureOpenAI => GetAzureOpenAIStatus(config),
            LlmProvider.Anthropic => GetAnthropicStatus(config),
            _ => new LlmConnectionStatus { IsConnected = false, ErrorMessage = "Unknown provider" }
        };
    }

    public LlmSettingsFromConfig GetConfigSettings()
    {
        var section = _configuration.GetSection("LlmSettings");

        return new LlmSettingsFromConfig
        {
            OllamaEndpoint = section.GetValue<string>("Ollama:Endpoint") ?? "http://localhost:11434",
            OllamaModel = section.GetValue<string>("Ollama:Model") ?? "llama3.2",

            OpenAIEndpoint = section.GetValue<string>("OpenAI:Endpoint") ?? "https://api.openai.com/v1",
            OpenAIModel = section.GetValue<string>("OpenAI:Model") ?? "gpt-4o",
            HasOpenAIApiKey = !string.IsNullOrWhiteSpace(section.GetValue<string>("OpenAI:ApiKey")),

            AzureEndpoint = section.GetValue<string>("AzureOpenAI:Endpoint") ?? "",
            AzureDeploymentName = section.GetValue<string>("AzureOpenAI:DeploymentName") ?? "",
            AzureApiVersion = section.GetValue<string>("AzureOpenAI:ApiVersion") ?? "2024-10-21",
            HasAzureApiKey = !string.IsNullOrWhiteSpace(section.GetValue<string>("AzureOpenAI:ApiKey")),
            HasApimSubscriptionKey = !string.IsNullOrWhiteSpace(section.GetValue<string>("AzureOpenAI:ApimSubscriptionKey")),
            UseAzureAD = section.GetValue<bool>("AzureOpenAI:UseAzureAD"),
            HasAzureADConfig = !string.IsNullOrWhiteSpace(section.GetValue<string>("AzureOpenAI:TenantId")) &&
                               !string.IsNullOrWhiteSpace(section.GetValue<string>("AzureOpenAI:ClientId")) &&
                               !string.IsNullOrWhiteSpace(section.GetValue<string>("AzureOpenAI:ClientSecret")),

            AnthropicEndpoint = section.GetValue<string>("Anthropic:Endpoint") ?? "https://api.anthropic.com",
            AnthropicModel = section.GetValue<string>("Anthropic:Model") ?? "claude-3-5-sonnet-20241022",
            HasAnthropicApiKey = !string.IsNullOrWhiteSpace(section.GetValue<string>("Anthropic:ApiKey")),

            TimeoutSeconds = section.GetValue<int?>("TimeoutSeconds") ?? 60
        };
    }

    private async Task<LlmProxyResponse> GenerateWithOllamaAsync(LlmProxyRequest request)
    {
        var section = _configuration.GetSection("LlmSettings:Ollama");
        var endpoint = section.GetValue<string>("Endpoint") ?? "http://localhost:11434";
        var model = section.GetValue<string>("Model") ?? "llama3.2";

        var ollamaRequest = new OllamaGenerateRequest
        {
            Model = model,
            Prompt = request.Prompt,
            System = request.SystemPrompt,
            Stream = false,
            Options = new OllamaOptions
            {
                Temperature = request.Temperature,
                NumPredict = request.MaxTokens
            }
        };

        var response = await _httpClient.PostAsJsonAsync($"{endpoint}/api/generate", ollamaRequest, SnakeCaseOptions);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(SnakeCaseOptions);
        return new LlmProxyResponse { Success = true, Response = result?.Response ?? "" };
    }

    private async Task<LlmProxyResponse> GenerateWithOpenAIAsync(LlmProxyRequest request)
    {
        var section = _configuration.GetSection("LlmSettings:OpenAI");
        var endpoint = section.GetValue<string>("Endpoint") ?? "https://api.openai.com/v1";
        var model = section.GetValue<string>("Model") ?? "gpt-4o";
        var apiKey = section.GetValue<string>("ApiKey");

        if (string.IsNullOrWhiteSpace(apiKey))
            return new LlmProxyResponse { Success = false, ErrorMessage = "OpenAI API key not configured" };

        var messages = new List<OpenAIChatMessage>();
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            messages.Add(new OpenAIChatMessage { Role = "system", Content = request.SystemPrompt });
        messages.Add(new OpenAIChatMessage { Role = "user", Content = request.Prompt });

        var openAiRequest = new OpenAIChatRequest
        {
            Model = model,
            Messages = messages,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/chat/completions");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(openAiRequest, CamelCaseOptions), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>(CamelCaseOptions);
        return new LlmProxyResponse { Success = true, Response = result?.Choices.FirstOrDefault()?.Message.Content ?? "" };
    }

    /// <summary>
    /// Azure OpenAI REST API — matches the official docs exactly:
    /// POST {endpoint}/openai/deployments/{deployment}/chat/completions?api-version=2024-10-21
    /// Headers: api-key, Content-Type: application/json
    /// </summary>
    private async Task<LlmProxyResponse> GenerateWithAzureOpenAIAsync(LlmProxyRequest request)
    {
        var section = _configuration.GetSection("LlmSettings:AzureOpenAI");
        var endpoint = section.GetValue<string>("Endpoint")?.Trim();
        var deploymentName = section.GetValue<string>("DeploymentName")?.Trim();
        var apiVersion = section.GetValue<string>("ApiVersion")?.Trim() ?? "2024-10-21";
        var apiKey = section.GetValue<string>("ApiKey")?.Trim();

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(deploymentName))
            return new LlmProxyResponse { Success = false, ErrorMessage = "Azure OpenAI: Endpoint or DeploymentName not configured" };
        if (string.IsNullOrEmpty(apiKey))
            return new LlmProxyResponse { Success = false, ErrorMessage = "Azure OpenAI: ApiKey not configured" };

        // URL — exactly per Azure OpenAI REST API docs
        var url = $"{endpoint.TrimEnd('/')}/openai/deployments/{deploymentName}/chat/completions?api-version={apiVersion}";

        // Request body — only the fields Azure OpenAI expects, no empty "model"
        var messages = new List<OpenAIChatMessage>();
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            messages.Add(new OpenAIChatMessage { Role = "system", Content = request.SystemPrompt });
        messages.Add(new OpenAIChatMessage { Role = "user", Content = request.Prompt });

        var body = new OpenAIChatRequest
        {
            Model = null!, // excluded via WhenWritingNull — Azure OpenAI uses deployment in URL
            Messages = messages,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens // Azure OpenAI 2024-10-21 uses max_tokens
        };

        var jsonBody = JsonSerializer.Serialize(body, SnakeCaseOptions);

        // HTTP request — just api-key + Content-Type, exactly like the docs
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.TryAddWithoutValidation("api-key", apiKey);
        httpRequest.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        _logger.LogInformation("Azure OpenAI POST {Url} | api-key: {KeyPreview}({KeyLen} chars) | body: {Body}",
            url, apiKey.Length > 4 ? apiKey[..4] + "..." : "***", apiKey.Length, jsonBody);

        var response = await _httpClient.SendAsync(httpRequest);

        // Handle redirect explicitly (AllowAutoRedirect is disabled)
        if ((int)response.StatusCode is >= 300 and < 400)
        {
            var location = response.Headers.Location?.ToString() ?? "(none)";
            return new LlmProxyResponse { Success = false, ErrorMessage = $"Redirect {(int)response.StatusCode} → {location}. Check Endpoint URL." };
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Azure OpenAI {Status}: {Error}", response.StatusCode, errorContent);
            return new LlmProxyResponse { Success = false, ErrorMessage = $"Azure OpenAI {(int)response.StatusCode}: {errorContent}" };
        }

        var result = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>(CamelCaseOptions);
        return new LlmProxyResponse { Success = true, Response = result?.Choices.FirstOrDefault()?.Message.Content ?? "" };
    }

    private async Task<LlmProxyResponse> GenerateWithAnthropicAsync(LlmProxyRequest request)
    {
        var section = _configuration.GetSection("LlmSettings:Anthropic");
        var endpoint = section.GetValue<string>("Endpoint") ?? "https://api.anthropic.com";
        var model = section.GetValue<string>("Model") ?? "claude-3-5-sonnet-20241022";
        var apiKey = section.GetValue<string>("ApiKey");

        if (string.IsNullOrWhiteSpace(apiKey))
            return new LlmProxyResponse { Success = false, ErrorMessage = "Anthropic API key not configured" };

        var anthropicRequest = new AnthropicMessageRequest
        {
            Model = model,
            Messages = new List<AnthropicMessage>
            {
                new AnthropicMessage { Role = "user", Content = request.Prompt }
            },
            System = request.SystemPrompt,
            MaxTokens = request.MaxTokens,
            Temperature = request.Temperature
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/v1/messages");
        httpRequest.Headers.Add("x-api-key", apiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(anthropicRequest, SnakeCaseOptions), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AnthropicMessageResponse>(SnakeCaseOptions);
        return new LlmProxyResponse { Success = true, Response = result?.Content.FirstOrDefault()?.Text ?? "" };
    }

    private async Task<LlmConnectionStatus> GetOllamaStatusAsync(LlmSettingsFromConfig config)
    {
        var status = new LlmConnectionStatus
        {
            Endpoint = config.OllamaEndpoint,
            Model = config.OllamaModel
        };

        try
        {
            var response = await _httpClient.GetAsync($"{config.OllamaEndpoint}/api/tags");
            if (response.IsSuccessStatusCode)
            {
                status.IsConnected = true;
                var tagsResponse = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(SnakeCaseOptions);
                status.AvailableModels = tagsResponse?.Models.Select(m => m.Name).ToList() ?? new List<string>();
            }
            else
            {
                status.ErrorMessage = $"Server returned {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            status.ErrorMessage = ex.Message;
        }

        return status;
    }

    private LlmConnectionStatus GetOpenAIStatus(LlmSettingsFromConfig config)
    {
        return new LlmConnectionStatus
        {
            IsConnected = config.HasOpenAIApiKey,
            Endpoint = config.OpenAIEndpoint,
            Model = config.OpenAIModel,
            ErrorMessage = config.HasOpenAIApiKey ? null : "API key not configured",
            AvailableModels = config.HasOpenAIApiKey
                ? new List<string> { "gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "gpt-4", "gpt-3.5-turbo" }
                : new List<string>()
        };
    }

    private LlmConnectionStatus GetAzureOpenAIStatus(LlmSettingsFromConfig config)
    {
        var hasEndpoint = !string.IsNullOrWhiteSpace(config.AzureEndpoint) &&
                          !string.IsNullOrWhiteSpace(config.AzureDeploymentName);
        var hasKey = config.HasAzureApiKey;
        var isConfigured = hasEndpoint && hasKey;

        string? errorMessage = null;
        if (!hasEndpoint)
            errorMessage = "Azure OpenAI: Endpoint or DeploymentName not configured";
        else if (!hasKey)
            errorMessage = "Azure OpenAI: ApiKey not configured";

        return new LlmConnectionStatus
        {
            IsConnected = isConfigured,
            Endpoint = config.AzureEndpoint,
            Model = config.AzureDeploymentName,
            ErrorMessage = errorMessage,
            AvailableModels = isConfigured ? new List<string> { config.AzureDeploymentName } : new List<string>()
        };
    }

    private LlmConnectionStatus GetAnthropicStatus(LlmSettingsFromConfig config)
    {
        return new LlmConnectionStatus
        {
            IsConnected = config.HasAnthropicApiKey,
            Endpoint = config.AnthropicEndpoint,
            Model = config.AnthropicModel,
            ErrorMessage = config.HasAnthropicApiKey ? null : "API key not configured",
            AvailableModels = config.HasAnthropicApiKey
                ? new List<string> { "claude-3-5-sonnet-20241022", "claude-3-5-haiku-20241022", "claude-3-opus-20240229" }
                : new List<string>()
        };
    }
}
