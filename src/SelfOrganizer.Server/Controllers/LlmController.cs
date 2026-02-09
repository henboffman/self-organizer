using System.Text;
using Microsoft.AspNetCore.Mvc;
using SelfOrganizer.Core.Models;
using SelfOrganizer.Server.Services;

namespace SelfOrganizer.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LlmController : ControllerBase
{
    private readonly ILlmProxyService _llmProxyService;
    private readonly IConfiguration _configuration;

    public LlmController(ILlmProxyService llmProxyService, IConfiguration configuration)
    {
        _llmProxyService = llmProxyService;
        _configuration = configuration;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<LlmProxyResponse>> Generate([FromBody] LlmProxyRequest request)
    {
        var response = await _llmProxyService.GenerateAsync(request);

        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpGet("status/{provider}")]
    public async Task<ActionResult<LlmConnectionStatus>> GetConnectionStatus(LlmProvider provider)
    {
        var status = await _llmProxyService.GetConnectionStatusAsync(provider);
        return Ok(status);
    }

    [HttpGet("config")]
    public ActionResult<LlmSettingsFromConfig> GetConfigSettings()
    {
        var config = _llmProxyService.GetConfigSettings();
        return Ok(config);
    }

    /// <summary>
    /// Diagnostic endpoint: makes a raw HTTP call to the Azure OpenAI APIM endpoint
    /// using a brand-new HttpClient (no DI). Returns full details of what was sent
    /// and received. Navigate to /api/llm/test-azure in the browser to run.
    /// </summary>
    [HttpGet("test-azure")]
    public async Task<ActionResult> TestAzureConnection()
    {
        var section = _configuration.GetSection("LlmSettings:AzureOpenAI");
        var endpoint = section.GetValue<string>("Endpoint")?.Trim();
        var deploymentName = section.GetValue<string>("DeploymentName")?.Trim();
        var apiVersion = section.GetValue<string>("ApiVersion")?.Trim() ?? "2024-10-21";
        var apiKey = section.GetValue<string>("ApiKey")?.Trim();
        var apimSubscriptionKey = section.GetValue<string>("ApimSubscriptionKey")?.Trim();

        var subscriptionKey = !string.IsNullOrEmpty(apimSubscriptionKey) ? apimSubscriptionKey : apiKey;
        var keySource = !string.IsNullOrEmpty(apimSubscriptionKey) ? "ApimSubscriptionKey" : "ApiKey";

        var diag = new Dictionary<string, object?>
        {
            ["config"] = new
            {
                endpoint,
                deploymentName,
                apiVersion,
                hasApiKey = !string.IsNullOrEmpty(apiKey),
                apiKeyLength = apiKey?.Length ?? 0,
                apiKeyPreview = apiKey?.Length > 4 ? apiKey[..4] + "..." : "(empty)",
                hasApimSubscriptionKey = !string.IsNullOrEmpty(apimSubscriptionKey),
                apimKeyLength = apimSubscriptionKey?.Length ?? 0,
                apimKeyPreview = apimSubscriptionKey?.Length > 4 ? apimSubscriptionKey[..4] + "..." : "(empty)",
                effectiveKeySource = keySource,
                effectiveKeyLength = subscriptionKey?.Length ?? 0,
            }
        };

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(deploymentName) || string.IsNullOrEmpty(subscriptionKey))
        {
            diag["error"] = "Missing required config. Need Endpoint, DeploymentName, and a subscription key.";
            return Ok(diag);
        }

        // Build a minimal chat completions request
        var url = $"{endpoint.TrimEnd('/')}/openai/deployments/{deploymentName}/chat/completions?api-version={apiVersion}";
        var body = """{"messages":[{"role":"user","content":"say hello"}],"max_completion_tokens":5}""";

        diag["requestUrl"] = url;
        diag["requestBody"] = body;

        // Use a completely fresh HttpClient — no DI, no factory, no middleware
        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var client = new HttpClient(handler);

        // api-key header — the standard Azure OpenAI auth header per MS docs
        client.DefaultRequestHeaders.TryAddWithoutValidation("api-key", subscriptionKey);

        var sentHeaders = new Dictionary<string, string>();
        foreach (var h in client.DefaultRequestHeaders)
        {
            var val = h.Value.FirstOrDefault() ?? "";
            sentHeaders[h.Key] = val.Length > 8 ? val[..4] + $"...({val.Length} chars)" : "***";
        }
        diag["requestHeaders"] = sentHeaders;

        try
        {
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            var responseHeaders = new Dictionary<string, string>();
            foreach (var h in response.Headers)
                responseHeaders[h.Key] = string.Join(", ", h.Value);

            diag["responseStatus"] = (int)response.StatusCode;
            diag["responseStatusText"] = response.StatusCode.ToString();
            diag["responseHeaders"] = responseHeaders;
            diag["responseBody"] = responseBody.Length > 2000 ? responseBody[..2000] + "..." : responseBody;

            if (response.IsSuccessStatusCode)
                diag["result"] = "SUCCESS — connection works!";
            else
                diag["result"] = $"FAILED — {(int)response.StatusCode} {response.StatusCode}";
        }
        catch (Exception ex)
        {
            diag["result"] = "EXCEPTION";
            diag["exception"] = ex.Message;
            if (ex.InnerException != null)
                diag["innerException"] = ex.InnerException.Message;
        }

        return Ok(diag);
    }
}
