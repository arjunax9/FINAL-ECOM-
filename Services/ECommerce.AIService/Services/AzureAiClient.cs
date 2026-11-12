using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ECommerce.AIService.Configuration;
using ECommerce.AIService.Models;
using Microsoft.Extensions.Options;

namespace ECommerce.AIService.Services;

public interface IAzureAiClient
{
    Task<AzureChatCompletionResponse?> GetChatCompletionAsync(
        List<AzureChatMessage> messages,
        double temperature = 0.3,
        int maxTokens = 800,
        CancellationToken cancellationToken = default);

    Task<List<float[]>?> GetEmbeddingsAsync(
        List<string> inputs,
        CancellationToken cancellationToken = default);

    Task<float[]?> GetSingleEmbeddingAsync(
        string input,
        CancellationToken cancellationToken = default);

    Task<bool> PingAsync(CancellationToken cancellationToken = default);
}

public class AzureAiClient : IAzureAiClient
{
    private readonly HttpClient _httpClient;
    private readonly AzureAiOptions _options;
    private readonly ILogger<AzureAiClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AzureAiClient(
        HttpClient httpClient,
        IOptions<AzureAiOptions> options,
        ILogger<AzureAiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AzureChatCompletionResponse?> GetChatCompletionAsync(
        List<AzureChatMessage> messages,
        double temperature = 0.3,
        int maxTokens = 800,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_options.Endpoint.TrimEnd('/')}/openai/deployments/{_options.ChatDeploymentName}/chat/completions?api-version={_options.ApiVersion}";

        var payload = new AzureChatCompletionRequest
        {
            Messages = messages,
            Temperature = temperature,
            MaxTokens = maxTokens
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("api-key", _options.ApiKey);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Azure AI Chat Completion failed with status {StatusCode}: {Body}", response.StatusCode, responseBody);
                return null;
            }

            return JsonSerializer.Deserialize<AzureChatCompletionResponse>(responseBody, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception calling Azure AI Chat Completion endpoint");
            return null;
        }
    }

    public async Task<List<float[]>?> GetEmbeddingsAsync(
        List<string> inputs,
        CancellationToken cancellationToken = default)
    {
        if (inputs == null || inputs.Count == 0) return new List<float[]>();

        var url = $"{_options.Endpoint.TrimEnd('/')}/openai/deployments/{_options.EmbeddingDeploymentName}/embeddings?api-version={_options.ApiVersion}";

        var payload = new AzureEmbeddingRequest
        {
            Input = inputs
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("api-key", _options.ApiKey);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Azure AI Embeddings failed with status {StatusCode}: {Body}", response.StatusCode, responseBody);
                return null;
            }

            var result = JsonSerializer.Deserialize<AzureEmbeddingResponse>(responseBody, JsonOptions);
            return result?.Data.Select(d => d.Embedding.ToArray()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception calling Azure AI Embeddings endpoint");
            return null;
        }
    }

    public async Task<float[]?> GetSingleEmbeddingAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        var list = await GetEmbeddingsAsync(new List<string> { input }, cancellationToken);
        return list?.FirstOrDefault();
    }

    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await GetSingleEmbeddingAsync("health check", cancellationToken);
            return res != null && res.Length > 0;
        }
        catch
        {
            return false;
        }
    }
}
