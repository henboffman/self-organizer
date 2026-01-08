using SelfOrganizer.Core.Models;

namespace SelfOrganizer.Core.Interfaces;

public interface ILlmService
{
    Task<bool> IsAvailableAsync();
    Task<LlmConnectionStatus> GetConnectionStatusAsync();
    Task<string> GenerateAsync(string prompt, LlmOptions? options = null);
    Task<T?> GenerateStructuredAsync<T>(string prompt, LlmOptions? options = null) where T : class;
    Task<LlmSettings> GetSettingsAsync();
    Task SaveSettingsAsync(LlmSettings settings);
    Task<List<string>> GetAvailableModelsAsync();

    /// <summary>
    /// Gets LLM settings directly from configuration (synchronous - returns defaults).
    /// Use GetConfigSettingsAsync for actual server-side configuration.
    /// </summary>
    LlmSettingsFromConfig GetConfigSettings();

    /// <summary>
    /// Gets LLM settings from server configuration asynchronously.
    /// Used by Settings UI to show what's configured without exposing actual API keys.
    /// </summary>
    Task<LlmSettingsFromConfig> GetConfigSettingsAsync();
}
