namespace ECommerce.AIService.Configuration;

public class AzureAiOptions
{
    public const string SectionName = "AzureAi";

    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChatDeploymentName { get; set; } = "gpt-4.1-mini";
    public string EmbeddingDeploymentName { get; set; } = "text-embedding-3-small";
    public string ApiVersion { get; set; } = "2024-06-01";
    public string WorkspaceProjectId { get; set; } = "proj-default";
    public string WorkspaceUrl { get; set; } = string.Empty;
}

public class ServiceUrlsOptions
{
    public const string SectionName = "Services";

    public string ProductServiceUrl { get; set; } = "http://localhost:5002/";
    public string OrderServiceUrl { get; set; } = "http://localhost:5003/";
}
