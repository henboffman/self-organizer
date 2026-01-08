namespace SelfOrganizer.Core.Models;

public class LlmOptions
{
    public string? Model { get; set; }
    public string? SystemPrompt { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2048;
    public int TimeoutSeconds { get; set; } = 60;
}

public class LlmConnectionStatus
{
    public bool IsConnected { get; set; }
    public string? Endpoint { get; set; }
    public string? Model { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> AvailableModels { get; set; } = new();
}

public enum LlmProvider
{
    Ollama,
    AzureOpenAI,
    OpenAI,
    Anthropic
}

public class LlmSettings
{
    public LlmProvider Provider { get; set; } = LlmProvider.Ollama;
    public bool IsEnabled { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 60;

    // Ollama settings
    public string OllamaEndpoint { get; set; } = "http://localhost:11434";
    public string OllamaModel { get; set; } = "llama3.2";

    // Azure OpenAI settings
    public string AzureEndpoint { get; set; } = string.Empty;
    public string AzureDeploymentName { get; set; } = string.Empty;
    public string AzureApiVersion { get; set; } = "2024-02-01";
    public string AzureApiKey { get; set; } = string.Empty;
    // For Azure AD authentication (optional)
    public string AzureTenantId { get; set; } = string.Empty;
    public string AzureClientId { get; set; } = string.Empty;
    public string AzureClientSecret { get; set; } = string.Empty;
    public string AzureScope { get; set; } = "https://cognitiveservices.azure.com/.default";
    public bool UseAzureADAuth { get; set; } = false;

    // OpenAI settings
    public string OpenAIApiKey { get; set; } = string.Empty;
    public string OpenAIModel { get; set; } = "gpt-4o";
    public string OpenAIEndpoint { get; set; } = "https://api.openai.com/v1";

    // Anthropic Claude settings
    public string AnthropicApiKey { get; set; } = string.Empty;
    public string AnthropicModel { get; set; } = "claude-3-5-sonnet-20241022";
    public string AnthropicEndpoint { get; set; } = "https://api.anthropic.com";

    // Legacy properties for backwards compatibility
    [Obsolete("Use OllamaEndpoint instead")]
    public string Endpoint
    {
        get => OllamaEndpoint;
        set => OllamaEndpoint = value;
    }

    [Obsolete("Use OllamaModel instead")]
    public string Model
    {
        get => OllamaModel;
        set => OllamaModel = value;
    }
}

public class OllamaGenerateRequest
{
    public string Model { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string? System { get; set; }
    public bool Stream { get; set; } = false;
    public OllamaOptions? Options { get; set; }
}

public class OllamaOptions
{
    public double? Temperature { get; set; }
    public int? NumPredict { get; set; }
}

public class OllamaGenerateResponse
{
    public string Model { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public bool Done { get; set; }
    public long? TotalDuration { get; set; }
}

public class OllamaTagsResponse
{
    public List<OllamaModel> Models { get; set; } = new();
}

public class OllamaModel
{
    public string Name { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public long Size { get; set; }
}

// OpenAI API models
public class OpenAIChatRequest
{
    public string Model { get; set; } = string.Empty;
    public List<OpenAIChatMessage> Messages { get; set; } = new();
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
}

public class OpenAIChatMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
}

public class OpenAIChatResponse
{
    public string Id { get; set; } = string.Empty;
    public List<OpenAIChatChoice> Choices { get; set; } = new();
    public OpenAIUsage? Usage { get; set; }
}

public class OpenAIChatChoice
{
    public OpenAIChatMessage Message { get; set; } = new();
    public string? FinishReason { get; set; }
}

public class OpenAIUsage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}

// Anthropic Claude API models
public class AnthropicMessageRequest
{
    public string Model { get; set; } = string.Empty;
    public List<AnthropicMessage> Messages { get; set; } = new();
    public string? System { get; set; }
    public int MaxTokens { get; set; } = 2048;
    public double? Temperature { get; set; }
}

public class AnthropicMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
}

public class AnthropicMessageResponse
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public List<AnthropicContentBlock> Content { get; set; } = new();
    public string? StopReason { get; set; }
    public AnthropicUsage? Usage { get; set; }
}

public class AnthropicContentBlock
{
    public string Type { get; set; } = "text";
    public string Text { get; set; } = string.Empty;
}

public class AnthropicUsage
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
}

/// <summary>
/// Read-only view of LLM settings from configuration (appsettings.json).
/// Used by Settings UI to display what's configured without exposing actual API keys.
/// </summary>
public class LlmSettingsFromConfig
{
    // Ollama
    public string OllamaEndpoint { get; set; } = "http://localhost:11434";
    public string OllamaModel { get; set; } = "llama3.2";

    // OpenAI
    public string OpenAIEndpoint { get; set; } = "https://api.openai.com/v1";
    public string OpenAIModel { get; set; } = "gpt-4o";
    public bool HasOpenAIApiKey { get; set; }

    // Azure OpenAI
    public string AzureEndpoint { get; set; } = string.Empty;
    public string AzureDeploymentName { get; set; } = string.Empty;
    public string AzureApiVersion { get; set; } = "2024-02-01";
    public bool HasAzureApiKey { get; set; }
    public bool HasApimSubscriptionKey { get; set; }
    public bool UseAzureAD { get; set; }
    public bool HasAzureADConfig { get; set; }

    // Anthropic
    public string AnthropicEndpoint { get; set; } = "https://api.anthropic.com";
    public string AnthropicModel { get; set; } = "claude-3-5-sonnet-20241022";
    public bool HasAnthropicApiKey { get; set; }

    public int TimeoutSeconds { get; set; } = 60;
}
