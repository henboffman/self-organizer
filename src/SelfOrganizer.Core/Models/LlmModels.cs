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

public class LlmSettings
{
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.2";
    public bool IsEnabled { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 60;
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
