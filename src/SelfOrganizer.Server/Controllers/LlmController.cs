using Microsoft.AspNetCore.Mvc;
using SelfOrganizer.Core.Models;
using SelfOrganizer.Server.Services;

namespace SelfOrganizer.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LlmController : ControllerBase
{
    private readonly ILlmProxyService _llmProxyService;

    public LlmController(ILlmProxyService llmProxyService)
    {
        _llmProxyService = llmProxyService;
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
}
