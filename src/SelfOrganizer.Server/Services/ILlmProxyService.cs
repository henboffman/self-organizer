using SelfOrganizer.Core.Models;

namespace SelfOrganizer.Server.Services;

public interface ILlmProxyService
{
    Task<LlmProxyResponse> GenerateAsync(LlmProxyRequest request);
    Task<LlmConnectionStatus> GetConnectionStatusAsync(LlmProvider provider);
    LlmSettingsFromConfig GetConfigSettings();
}

public class LlmProxyRequest
{
    public LlmProvider Provider { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string? SystemPrompt { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2048;
}

public class LlmProxyResponse
{
    public bool Success { get; set; }
    public string Response { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
