using System.Text.Json.Serialization;

namespace ECommerce.AIService.Models;

public class AiModelInfo
{
    public string Name { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string DeploymentType { get; set; } = "Global Standard";
    public string Status { get; set; } = "Succeeded";
    public string Capabilities { get; set; } = string.Empty;
    public int? ContextWindowTokens { get; set; }
    public int? Dimensions { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class WorkspaceOverview
{
    public string ProjectId { get; set; } = "proj-default";
    public string WorkspaceUrl { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public List<AiModelInfo> DeployedModels { get; set; } = new();
}

public class ProductCatalogItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class RagDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "Product", "Policy", "FAQ"
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public float[]? Embedding { get; set; }
}

public class RagSearchResult
{
    public RagDocument Document { get; set; } = null!;
    public float SimilarityScore { get; set; }
}

public class ContentSafetyCheckResult
{
    public bool IsSafe { get; set; } = true;
    public bool JailbreakDetected { get; set; }
    public bool ToxicityDetected { get; set; }
    public bool PiiDetected { get; set; }
    public string MaskedMessage { get; set; } = string.Empty;
    public List<string> Violations { get; set; } = new();
    public string RejectionReason { get; set; } = string.Empty;
}

public class GovernanceAuditLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int? UserId { get; set; }
    public string UserMessageMasked { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public string ActionTaken { get; set; } = "Allowed"; // "Allowed", "Refused", "Redacted"
    public bool JailbreakAttempt { get; set; }
    public bool PiiSanitized { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public long LatencyMs { get; set; }
    public string ModelUsed { get; set; } = "gpt-4.1-mini";
}

// Low-level Azure OpenAI REST DTOs
public class AzureChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user"; // "system", "user", "assistant"

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class AzureChatCompletionRequest
{
    [JsonPropertyName("messages")]
    public List<AzureChatMessage> Messages { get; set; } = new();

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.3;

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 800;
}

public class AzureChatCompletionResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<AzureChatChoice> Choices { get; set; } = new();

    [JsonPropertyName("usage")]
    public AzureUsage? Usage { get; set; }
}

public class AzureChatChoice
{
    [JsonPropertyName("message")]
    public AzureChatMessage Message { get; set; } = new();

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; } = string.Empty;
}

public class AzureUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

public class AzureEmbeddingRequest
{
    [JsonPropertyName("input")]
    public object Input { get; set; } = string.Empty; // string or string[]
}

public class AzureEmbeddingResponse
{
    [JsonPropertyName("data")]
    public List<AzureEmbeddingData> Data { get; set; } = new();
}

public class AzureEmbeddingData
{
    [JsonPropertyName("embedding")]
    public List<float> Embedding { get; set; } = new();

    [JsonPropertyName("index")]
    public int Index { get; set; }
}
