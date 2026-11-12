using ECommerce.AIService.Configuration;
using ECommerce.AIService.Models;
using ECommerce.AIService.Services;
using ECommerce.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ECommerce.AIService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiChatController : ControllerBase
{
    private readonly IAiAgentOrchestrator _orchestrator;
    private readonly IProductRecommendationAgent _recommendationAgent;
    private readonly IRagService _ragService;
    private readonly ILogger<AiChatController> _logger;

    public AiChatController(
        IAiAgentOrchestrator orchestrator,
        IProductRecommendationAgent recommendationAgent,
        IRagService ragService,
        ILogger<AiChatController> logger)
    {
        _orchestrator = orchestrator;
        _recommendationAgent = recommendationAgent;
        _ragService = ragService;
        _logger = logger;
    }

    [HttpPost]
    [Route("")]
    [Route("/api/chat")]
    [Route("/api/ai/chat")]
    public async Task<ActionResult<ChatMessageResponse>> Chat([FromBody] ChatMessageRequest request, CancellationToken cancellationToken)
    {
        var response = await _orchestrator.ProcessAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [Route("/api/ai/recommend")]
    public async Task<ActionResult<ChatMessageResponse>> Recommend([FromBody] ChatMessageRequest request, CancellationToken cancellationToken)
    {
        var groundedContext = await _ragService.BuildGroundedContextAsync(request.Message, topK: 3, cancellationToken);
        var response = await _recommendationAgent.GenerateRecommendationsAsync(request.Message, groundedContext, cancellationToken);
        return Ok(response);
    }
}

[ApiController]
[Route("api/ai/rag")]
public class RagController : ControllerBase
{
    private readonly IRagService _ragService;

    public RagController(IRagService ragService)
    {
        _ragService = ragService;
    }

    public record RagSearchRequest(string Query, int? TopK);

    [HttpPost("search")]
    public async Task<ActionResult<List<RagSearchResult>>> Search([FromBody] RagSearchRequest request, CancellationToken cancellationToken)
    {
        var results = await _ragService.SearchAsync(request.Query, request.TopK ?? 4, minSimilarity: 0.20f, cancellationToken);
        return Ok(results);
    }

    [HttpPost("reindex")]
    public async Task<IActionResult> Reindex(CancellationToken cancellationToken)
    {
        await _ragService.ReindexAsync(cancellationToken);
        return Ok(new { message = "Reindexing complete", totalDocuments = _ragService.TotalIndexedDocuments });
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            totalDocuments = _ragService.TotalIndexedDocuments,
            embeddingModel = "text-embedding-3-small",
            dimensions = 1536
        });
    }
}

[ApiController]
[Route("api/ai/models")]
public class ModelCatalogController : ControllerBase
{
    private readonly IAzureAiClient _azureAiClient;
    private readonly AzureAiOptions _options;

    public ModelCatalogController(
        IAzureAiClient azureAiClient,
        IOptions<AzureAiOptions> options)
    {
        _azureAiClient = azureAiClient;
        _options = options.Value;
    }

    [HttpGet]
    public async Task<ActionResult<WorkspaceOverview>> GetCatalog(CancellationToken cancellationToken)
    {
        var isConnected = await _azureAiClient.PingAsync(cancellationToken);

        var overview = new WorkspaceOverview
        {
            ProjectId = _options.WorkspaceProjectId,
            WorkspaceUrl = _options.WorkspaceUrl,
            Endpoint = _options.Endpoint,
            IsConnected = isConnected,
            DeployedModels = new List<AiModelInfo>
            {
                new()
                {
                    Name = _options.ChatDeploymentName,
                    ModelId = "gpt-4.1-mini",
                    Version = "2025-04-14",
                    DeploymentType = "Global Standard",
                    Status = "Succeeded",
                    Capabilities = "Chat, Tool Calling, JSON Mode, System Guardrails, Content Safety Filter",
                    ContextWindowTokens = 128000,
                    Description = "Next-generation compact multimodal reasoning model optimized for low-latency agentic customer interactions."
                },
                new()
                {
                    Name = _options.EmbeddingDeploymentName,
                    ModelId = "text-embedding-3-small",
                    Version = "1",
                    DeploymentType = "Global Standard",
                    Status = "Succeeded",
                    Capabilities = "Text Embeddings, Semantic Search, Cosine Similarity Retrieval",
                    Dimensions = 1536,
                    Description = "High-efficiency semantic representation model for enterprise RAG vector stores."
                }
            }
        };

        return Ok(overview);
    }
}

[ApiController]
[Route("api/ai/governance")]
public class GovernanceController : ControllerBase
{
    private readonly IGovernanceService _governanceService;

    public GovernanceController(IGovernanceService governanceService)
    {
        _governanceService = governanceService;
    }

    [HttpGet("audit")]
    public ActionResult<List<GovernanceAuditLog>> GetAuditTrail([FromQuery] int count = 50)
    {
        var logs = _governanceService.GetRecentAuditLogs(count);
        return Ok(logs);
    }

    public record GovernanceTestRequest(string Message);

    [HttpPost("evaluate")]
    public ActionResult<ContentSafetyCheckResult> Evaluate([FromBody] GovernanceTestRequest request)
    {
        var result = _governanceService.EvaluateInput(request.Message);
        return Ok(result);
    }
}
