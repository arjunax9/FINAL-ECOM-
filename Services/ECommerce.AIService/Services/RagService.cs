using System.Collections.Concurrent;
using ECommerce.AIService.Clients;
using ECommerce.AIService.Models;

namespace ECommerce.AIService.Services;

public interface IRagService
{
    Task InitializeIndexAsync(CancellationToken cancellationToken = default);
    Task<List<RagSearchResult>> SearchAsync(string query, int topK = 4, float minSimilarity = 0.25f, CancellationToken cancellationToken = default);
    Task<string> BuildGroundedContextAsync(string query, int topK = 3, CancellationToken cancellationToken = default);
    Task ReindexAsync(CancellationToken cancellationToken = default);
    int TotalIndexedDocuments { get; }
}

public class RagService : IRagService
{
    private readonly IAzureAiClient _azureAiClient;
    private readonly IProductServiceClient _productServiceClient;
    private readonly ILogger<RagService> _logger;
    private readonly List<RagDocument> _documents = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _isInitialized = false;

    public int TotalIndexedDocuments => _documents.Count;

    public RagService(
        IAzureAiClient azureAiClient,
        IProductServiceClient productServiceClient,
        ILogger<RagService> logger)
    {
        _azureAiClient = azureAiClient;
        _productServiceClient = productServiceClient;
        _logger = logger;
    }

    public async Task InitializeIndexAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized) return;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized) return;
            await BuildKnowledgeBaseAndEmbeddingsAsync(cancellationToken);
            _isInitialized = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ReindexAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _documents.Clear();
            _isInitialized = false;
            await BuildKnowledgeBaseAndEmbeddingsAsync(cancellationToken);
            _isInitialized = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<RagSearchResult>> SearchAsync(
        string query,
        int topK = 4,
        float minSimilarity = 0.25f,
        CancellationToken cancellationToken = default)
    {
        await InitializeIndexAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(query) || _documents.Count == 0)
        {
            return new List<RagSearchResult>();
        }

        var queryEmbedding = await _azureAiClient.GetSingleEmbeddingAsync(query, cancellationToken);
        if (queryEmbedding == null || queryEmbedding.Length == 0)
        {
            _logger.LogWarning("Failed to generate embedding for query: {Query}, falling back to keyword search", query);
            return FallbackKeywordSearch(query, topK);
        }

        var results = new List<RagSearchResult>();

        foreach (var doc in _documents)
        {
            if (doc.Embedding == null || doc.Embedding.Length == 0) continue;

            var similarity = ComputeCosineSimilarity(queryEmbedding, doc.Embedding);
            if (similarity >= minSimilarity)
            {
                results.Add(new RagSearchResult
                {
                    Document = doc,
                    SimilarityScore = (float)Math.Round(similarity, 4)
                });
            }
        }

        return results.OrderByDescending(r => r.SimilarityScore).Take(topK).ToList();
    }

    public async Task<string> BuildGroundedContextAsync(
        string query,
        int topK = 3,
        CancellationToken cancellationToken = default)
    {
        var searchResults = await SearchAsync(query, topK, 0.25f, cancellationToken);
        if (searchResults.Count == 0)
        {
            return "No specific enterprise documentation matched this query.";
        }

        var builder = new System.Text.StringBuilder();
        builder.AppendLine("### VERIFIED ENTERPRISE KNOWLEDGE BASE CHUNKS (RAG RETRIEVAL):");
        foreach (var item in searchResults)
        {
            builder.AppendLine($"\n[Source: {item.Document.Category} - {item.Document.Title} (Relevance Score: {item.SimilarityScore:P0})]");
            builder.AppendLine(item.Document.Content);
        }

        return builder.ToString();
    }

    private async Task BuildKnowledgeBaseAndEmbeddingsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Building Enterprise Knowledge Base for RAG vector index...");

        var rawDocs = new List<RagDocument>();

        // 1. Enterprise Policies Knowledge
        rawDocs.Add(new RagDocument
        {
            Title = "Shipping & Delivery Policy",
            Category = "Policy",
            Content = "Shipping Options & Delivery Estimates:\n" +
                      "- Standard Shipping: 3 to 5 business days across the country ($5.99 flat rate or FREE for orders over $50).\n" +
                      "- Express Shipping: 1 to 2 business days ($14.99 or FREE for orders over $150).\n" +
                      "- Real-time order tracking is available via Order ID (e.g. 'track #12'). All shipments are insured.",
            Metadata = new() { ["type"] = "shipping", ["free_threshold"] = "50" }
        });

        rawDocs.Add(new RagDocument
        {
            Title = "30-Day Hassle-Free Returns & Refund Policy",
            Category = "Policy",
            Content = "Returns & Refunds Terms:\n" +
                      "- Customers have 30 days from the delivery date to return eligible items in original packaging.\n" +
                      "- Prepaid shipping return labels are provided at no extra cost for defective or unsatisfactory items.\n" +
                      "- Refunds are credited to the original payment method within 3 to 5 business days after inspection.",
            Metadata = new() { ["type"] = "returns", ["window_days"] = "30" }
        });

        rawDocs.Add(new RagDocument
        {
            Title = "Payment Methods & Transaction Security",
            Category = "Policy",
            Content = "Payment & Security Standards:\n" +
                      "- Accepted cards: Visa, MasterCard, American Express, and Discover.\n" +
                      "- All checkout transactions are secured with industry-grade AES-256 and SSL/TLS encryption.\n" +
                      "- We never store raw credit card numbers or CVV codes.",
            Metadata = new() { ["type"] = "payment", ["encryption"] = "AES-256" }
        });

        rawDocs.Add(new RagDocument
        {
            Title = "Warranty & Hardware Replacement",
            Category = "Policy",
            Content = "Warranty Information:\n" +
                      "- All electronics and accessories include a 1-year manufacturer warranty covering hardware defects.\n" +
                      "- Free hardware replacement is expedited within 48 hours for verified defective units.",
            Metadata = new() { ["type"] = "warranty", ["period"] = "1 year" }
        });

        rawDocs.Add(new RagDocument
        {
            Title = "Order Cancellation Policy",
            Category = "Policy",
            Content = "Order Cancellations:\n" +
                      "- Customers can cancel an order immediately with full refund while status is 'Pending'.\n" +
                      "- Once an order is 'Paid' or 'In-Transit', our 30-day return policy applies upon delivery.",
            Metadata = new() { ["type"] = "cancellation" }
        });

        // 2. Enterprise Product Catalog Knowledge
        var products = await _productServiceClient.GetAllProductsAsync(cancellationToken);
        foreach (var p in products)
        {
            rawDocs.Add(new RagDocument
            {
                Title = p.Name,
                Category = "Product",
                Content = $"Product Name: {p.Name}\n" +
                          $"ID: {p.Id}\n" +
                          $"Category: {p.Category}\n" +
                          $"Price: ${p.Price:F2}\n" +
                          $"Current Stock: {p.Quantity} units ({(p.Quantity > 0 ? "In Stock" : "Out of Stock")})\n" +
                          $"Description: {p.Description}",
                Metadata = new()
                {
                    ["productId"] = p.Id.ToString(),
                    ["price"] = p.Price.ToString(),
                    ["category"] = p.Category,
                    ["quantity"] = p.Quantity.ToString()
                }
            });
        }

        // Generate vector embeddings in batch via Azure AI Foundry text-embedding-3-small
        var textsToEmbed = rawDocs.Select(d => $"{d.Title}\n{d.Category}\n{d.Content}").ToList();

        try
        {
            var embeddings = await _azureAiClient.GetEmbeddingsAsync(textsToEmbed, cancellationToken);
            if (embeddings != null && embeddings.Count == rawDocs.Count)
            {
                for (int i = 0; i < rawDocs.Count; i++)
                {
                    rawDocs[i].Embedding = embeddings[i];
                }
                _logger.LogInformation("Successfully generated 1536-d embeddings for {Count} enterprise documents using text-embedding-3-small", rawDocs.Count);
            }
            else
            {
                _logger.LogWarning("Azure AI returned incomplete embeddings ({Count}/{Expected}), falling back", embeddings?.Count ?? 0, rawDocs.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embeddings via Azure AI Foundry during indexing");
        }

        _documents.AddRange(rawDocs);
    }

    private static float ComputeCosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length || vectorA.Length == 0) return 0f;

        double dot = 0.0;
        double magA = 0.0;
        double magB = 0.0;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dot += vectorA[i] * vectorB[i];
            magA += vectorA[i] * vectorA[i];
            magB += vectorB[i] * vectorB[i];
        }

        if (magA <= 0.0 || magB <= 0.0) return 0f;
        return (float)(dot / (Math.Sqrt(magA) * Math.Sqrt(magB)));
    }

    private List<RagSearchResult> FallbackKeywordSearch(string query, int topK)
    {
        var terms = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var matches = new List<RagSearchResult>();

        foreach (var doc in _documents)
        {
            var docText = $"{doc.Title} {doc.Content}".ToLowerInvariant();
            int score = terms.Count(t => docText.Contains(t));
            if (score > 0)
            {
                matches.Add(new RagSearchResult
                {
                    Document = doc,
                    SimilarityScore = Math.Min(1.0f, score * 0.25f)
                });
            }
        }

        return matches.OrderByDescending(m => m.SimilarityScore).Take(topK).ToList();
    }
}
