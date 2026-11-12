using System.Diagnostics;
using System.Text.RegularExpressions;
using ECommerce.AIService.Clients;
using ECommerce.AIService.Models;
using ECommerce.Models.DTOs;
using ECommerce.Models.Entities;

namespace ECommerce.AIService.Services;

public interface ICustomerServiceAgent
{
    Task<ChatMessageResponse?> TryHandleCustomerServiceIntentAsync(
        ChatMessageRequest request,
        string groundedContext,
        CancellationToken cancellationToken = default);
}

public class CustomerServiceAgent : ICustomerServiceAgent
{
    private readonly IOrderServiceClient _orderServiceClient;
    private readonly ILogger<CustomerServiceAgent> _logger;

    public CustomerServiceAgent(
        IOrderServiceClient orderServiceClient,
        ILogger<CustomerServiceAgent> logger)
    {
        _orderServiceClient = orderServiceClient;
        _logger = logger;
    }

    public async Task<ChatMessageResponse?> TryHandleCustomerServiceIntentAsync(
        ChatMessageRequest request,
        string groundedContext,
        CancellationToken cancellationToken = default)
    {
        var msg = request.Message.ToLowerInvariant();

        // 1. Order lookup by ID
        var idMatch = Regex.Match(msg, @"(?:order|track|tracking|#)\s*#?\s*(\d+)");
        if (idMatch.Success && int.TryParse(idMatch.Groups[1].Value, out var orderId))
        {
            var order = await _orderServiceClient.GetOrderByIdAsync(orderId, cancellationToken);
            if (order != null)
            {
                var itemCount = order.OrderItems?.Count ?? 1;
                return new ChatMessageResponse
                {
                    Intent = "Order_Found",
                    Reply = $"✅ **Found Order #{order.Id}**!\n\n" +
                            $"• **Current Status**: `{order.Status}`\n" +
                            $"• **Total Amount**: `${order.TotalAmount:F2}`\n" +
                            $"• **Placed On**: {order.CreatedAt:MMM dd, yyyy HH:mm} UTC\n\n" +
                            GetStatusMessage(order.Status),
                    Order = new ChatOrderCard
                    {
                        Id = order.Id,
                        Status = order.Status,
                        TotalAmount = order.TotalAmount,
                        CreatedAt = order.CreatedAt,
                        ItemCount = itemCount
                    },
                    Suggestions = new List<string> { "Shipping Policy", "Return Policy", "Browse Products", "Contact Support" }
                };
            }
            else
            {
                return new ChatMessageResponse
                {
                    Intent = "Order_NotFound",
                    Reply = $"🔍 We couldn't find an order matching **#{orderId}**. Please verify the number from your confirmation email or order history.",
                    Suggestions = new List<string> { "Track Another Order", "View My Orders", "Contact Support" }
                };
            }
        }

        // 2. User's recent orders lookup
        if ((msg.Contains("my order") || msg.Contains("recent order") || msg.Contains("where is my order")) && request.UserId.HasValue)
        {
            var userOrders = await _orderServiceClient.GetOrdersByUserIdAsync(request.UserId.Value, cancellationToken);
            if (userOrders.Count > 0)
            {
                var latest = userOrders.OrderByDescending(o => o.CreatedAt).First();
                return new ChatMessageResponse
                {
                    Intent = "User_Orders",
                    Reply = $"📦 You have **{userOrders.Count}** order(s) on file.\n\n" +
                            $"Your most recent is **Order #{latest.Id}** placed on {latest.CreatedAt:MMM dd, yyyy}.\n" +
                            $"• **Status**: `{latest.Status}`\n" +
                            $"• **Total**: `${latest.TotalAmount:F2}`",
                    Order = new ChatOrderCard
                    {
                        Id = latest.Id,
                        Status = latest.Status,
                        TotalAmount = latest.TotalAmount,
                        CreatedAt = latest.CreatedAt,
                        ItemCount = latest.OrderItems?.Count ?? 1
                    },
                    Suggestions = new List<string> { $"Track #{latest.Id}", "Return Policy", "Browse Products" }
                };
            }
        }

        return null;
    }

    private static string GetStatusMessage(string status) => status switch
    {
        "Pending" => "⏳ Your order has been placed and is awaiting payment processing / fulfillment.",
        "CheckedOut" => "📦 Your order has been checked out and is being prepared for shipment.",
        "Paid" => "💳 Payment confirmed! Your package is queued for carrier pickup.",
        "Shipped" => "🚚 Your order is on its way with insured tracking.",
        "Delivered" => "🎉 Package delivered. Enjoy your purchase!",
        _ => $"Your order status is currently: {status}."
    };
}

public interface IProductRecommendationAgent
{
    Task<ChatMessageResponse> GenerateRecommendationsAsync(
        string userQuery,
        string groundedContext,
        CancellationToken cancellationToken = default);
}

public class ProductRecommendationAgent : IProductRecommendationAgent
{
    private readonly IProductServiceClient _productServiceClient;
    private readonly IRagService _ragService;
    private readonly IAzureAiClient _azureAiClient;
    private readonly IGovernanceService _governanceService;
    private readonly ILogger<ProductRecommendationAgent> _logger;

    public ProductRecommendationAgent(
        IProductServiceClient productServiceClient,
        IRagService ragService,
        IAzureAiClient azureAiClient,
        IGovernanceService governanceService,
        ILogger<ProductRecommendationAgent> logger)
    {
        _productServiceClient = productServiceClient;
        _ragService = ragService;
        _azureAiClient = azureAiClient;
        _governanceService = governanceService;
        _logger = logger;
    }

    public async Task<ChatMessageResponse> GenerateRecommendationsAsync(
        string userQuery,
        string groundedContext,
        CancellationToken cancellationToken = default)
    {
        // 1. Retrieve top matching products via semantic search
        var searchResults = await _ragService.SearchAsync(userQuery, topK: 3, minSimilarity: 0.20f, cancellationToken);
        var productDocs = searchResults.Where(r => r.Document.Category == "Product").ToList();

        // 2. Fetch full product objects for UI cards
        var allProducts = await _productServiceClient.GetAllProductsAsync(cancellationToken);
        var matchedProducts = new List<ChatProductCard>();

        foreach (var pDoc in productDocs)
        {
            if (pDoc.Document.Metadata.TryGetValue("productId", out var pidStr) && int.TryParse(pidStr, out var pid))
            {
                var prod = allProducts.FirstOrDefault(p => p.Id == pid);
                if (prod != null)
                {
                    matchedProducts.Add(new ChatProductCard
                    {
                        Id = prod.Id,
                        Name = prod.Name,
                        Description = prod.Description,
                        Price = prod.Price,
                        Category = prod.Category,
                        Quantity = prod.Quantity
                    });
                }
            }
        }

        // If no products matched RAG, pick top 2 in-stock items
        if (matchedProducts.Count == 0)
        {
            matchedProducts = allProducts.Where(p => p.Quantity > 0).Take(2).Select(prod => new ChatProductCard
            {
                Id = prod.Id,
                Name = prod.Name,
                Description = prod.Description,
                Price = prod.Price,
                Category = prod.Category,
                Quantity = prod.Quantity
            }).ToList();
        }

        // 3. Generate tailored AI recommendation copy via gpt-4.1-mini
        var systemPrompt = 
            "You are the Product Recommendation Specialist for our enterprise store.\n" +
            _governanceService.GetResponsibleSystemGuardrails() + "\n\n" +
            groundedContext + "\n\n" +
            "Task: Provide an engaging, helpful response recommending the most relevant product(s) for the customer's request. " +
            "Highlight key features, explain why it fits their needs, and note stock availability. Keep your answer under 120 words.";

        var messages = new List<AzureChatMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = userQuery }
        };

        var aiResponse = await _azureAiClient.GetChatCompletionAsync(messages, temperature: 0.4, maxTokens: 400, cancellationToken: cancellationToken);
        var replyText = aiResponse?.Choices.FirstOrDefault()?.Message.Content;

        if (string.IsNullOrWhiteSpace(replyText))
        {
            replyText = $"Here are our top recommended products tailored for **\"{userQuery}\"**:";
        }

        return new ChatMessageResponse
        {
            Intent = "Product_Recommendations",
            Reply = replyText.Trim(),
            Products = matchedProducts,
            Suggestions = new List<string> { "View Best Sellers", "Check Shipping Speeds", "Return Policy", "Contact Support" }
        };
    }
}

public interface IAiAgentOrchestrator
{
    Task<ChatMessageResponse> ProcessAsync(ChatMessageRequest request, CancellationToken cancellationToken = default);
}

public class AiAgentOrchestrator : IAiAgentOrchestrator
{
    private readonly IAzureAiClient _azureAiClient;
    private readonly IRagService _ragService;
    private readonly IGovernanceService _governanceService;
    private readonly ICustomerServiceAgent _customerServiceAgent;
    private readonly IProductRecommendationAgent _recommendationAgent;
    private readonly ILogger<AiAgentOrchestrator> _logger;

    public AiAgentOrchestrator(
        IAzureAiClient azureAiClient,
        IRagService ragService,
        IGovernanceService governanceService,
        ICustomerServiceAgent customerServiceAgent,
        IProductRecommendationAgent recommendationAgent,
        ILogger<AiAgentOrchestrator> logger)
    {
        _azureAiClient = azureAiClient;
        _ragService = ragService;
        _governanceService = governanceService;
        _customerServiceAgent = customerServiceAgent;
        _recommendationAgent = recommendationAgent;
        _logger = logger;
    }

    public async Task<ChatMessageResponse> ProcessAsync(ChatMessageRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var rawMessage = request.Message?.Trim() ?? string.Empty;

        // 1. Governance & Content Safety Check
        var safety = _governanceService.EvaluateInput(rawMessage);
        if (!safety.IsSafe)
        {
            stopwatch.Stop();
            _governanceService.RecordAudit(new GovernanceAuditLog
            {
                UserId = request.UserId,
                UserMessageMasked = safety.MaskedMessage,
                Intent = "Jailbreak_Blocked",
                ActionTaken = "Refused",
                JailbreakAttempt = true,
                LatencyMs = stopwatch.ElapsedMilliseconds
            });

            return new ChatMessageResponse
            {
                Intent = "Safety_Refusal",
                Reply = safety.RejectionReason,
                Suggestions = new List<string> { "Browse Products", "Track an Order", "Store Policies" }
            };
        }

        var activeMessage = safety.MaskedMessage;

        // 2. Empty / Greeting handling
        if (string.IsNullOrWhiteSpace(activeMessage))
        {
            var userGreeting = string.IsNullOrWhiteSpace(request.UserName) ? "there" : request.UserName;
            return new ChatMessageResponse
            {
                Intent = "Greeting",
                Reply = $"👋 Hello **{userGreeting}**! I am your AI Shopping & Customer Service Assistant powered by Microsoft AI Foundry.\n\n" +
                        "How can I assist you today? I can help you find products, track orders, or answer shipping & policy questions.",
                Suggestions = new List<string> { "Recommend Electronics", "Track My Order", "Shipping Times", "30-Day Return Policy" }
            };
        }

        // 3. RAG Context Retrieval
        var groundedContext = await _ragService.BuildGroundedContextAsync(activeMessage, topK: 3, cancellationToken: cancellationToken);

        // 4. Try specialized Customer Service Agent (e.g. order tracking)
        var csResult = await _customerServiceAgent.TryHandleCustomerServiceIntentAsync(request, groundedContext, cancellationToken);
        if (csResult != null)
        {
            stopwatch.Stop();
            _governanceService.RecordAudit(new GovernanceAuditLog
            {
                UserId = request.UserId,
                UserMessageMasked = activeMessage,
                Intent = csResult.Intent,
                ActionTaken = safety.PiiDetected ? "Redacted" : "Allowed",
                PiiSanitized = safety.PiiDetected,
                LatencyMs = stopwatch.ElapsedMilliseconds
            });
            return csResult;
        }

        // 5. Try Product Recommendation Agent if intent looks like shopping/recommendation
        var lower = activeMessage.ToLowerInvariant();
        bool isRecommendationIntent = 
            lower.Contains("recommend") || lower.Contains("suggest") || lower.Contains("looking for") ||
            lower.Contains("headphone") || lower.Contains("keyboard") || lower.Contains("monitor") ||
            lower.Contains("chair") || lower.Contains("bottle") || lower.Contains("watch") ||
            lower.Contains("best") || lower.Contains("buy") || lower.Contains("shop") || lower.Contains("gift");

        if (isRecommendationIntent)
        {
            var recResult = await _recommendationAgent.GenerateRecommendationsAsync(activeMessage, groundedContext, cancellationToken);
            stopwatch.Stop();
            _governanceService.RecordAudit(new GovernanceAuditLog
            {
                UserId = request.UserId,
                UserMessageMasked = activeMessage,
                Intent = recResult.Intent,
                ActionTaken = safety.PiiDetected ? "Redacted" : "Allowed",
                PiiSanitized = safety.PiiDetected,
                LatencyMs = stopwatch.ElapsedMilliseconds
            });
            return recResult;
        }

        // 6. General RAG-Grounded AI Conversational Agent via gpt-4.1-mini
        var systemPrompt = 
            "You are the Intelligent Customer Service Assistant for our enterprise e-commerce platform.\n" +
            _governanceService.GetResponsibleSystemGuardrails() + "\n\n" +
            groundedContext + "\n\n" +
            "Provide a helpful, precise, and friendly answer strictly based on the enterprise knowledge above. " +
            "If the information is not in the knowledge base, advise the user that our support team is available.";

        var chatMessages = new List<AzureChatMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = activeMessage }
        };

        var aiResponse = await _azureAiClient.GetChatCompletionAsync(chatMessages, temperature: 0.3, maxTokens: 500, cancellationToken: cancellationToken);
        var replyText = aiResponse?.Choices.FirstOrDefault()?.Message.Content;

        if (string.IsNullOrWhiteSpace(replyText))
        {
            replyText = "I'm here to help with all your store inquiries. You can ask me to track orders, recommend products, or explain our shipping and return policies.";
        }

        // Post-generation output sanitization
        replyText = _governanceService.SanitizeOutput(replyText);

        stopwatch.Stop();
        _governanceService.RecordAudit(new GovernanceAuditLog
        {
            UserId = request.UserId,
            UserMessageMasked = activeMessage,
            Intent = "General_Inquiry",
            ActionTaken = safety.PiiDetected ? "Redacted" : "Allowed",
            PiiSanitized = safety.PiiDetected,
            PromptTokens = aiResponse?.Usage?.PromptTokens ?? 0,
            CompletionTokens = aiResponse?.Usage?.CompletionTokens ?? 0,
            LatencyMs = stopwatch.ElapsedMilliseconds
        });

        return new ChatMessageResponse
        {
            Intent = "FAQ_Policy",
            Reply = replyText.Trim(),
            Suggestions = new List<string> { "Browse Products", "Track an Order", "Return Policy", "Contact Support" }
        };
    }
}
