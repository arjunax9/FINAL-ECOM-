using System.Text.RegularExpressions;
using ECommerce.Models.DTOs;
using ECommerce.OrderService.Clients;
using ECommerce.OrderService.Repositories;

namespace ECommerce.OrderService.Services;

public class ChatbotService : IChatbotService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductCatalogClient _productCatalogClient;
    private readonly ILogger<ChatbotService> _logger;

    public ChatbotService(
        IOrderRepository orderRepository,
        IProductCatalogClient productCatalogClient,
        ILogger<ChatbotService> logger)
    {
        _orderRepository = orderRepository;
        _productCatalogClient = productCatalogClient;
        _logger = logger;
    }

    public async Task<ChatMessageResponse> ProcessMessageAsync(ChatMessageRequest request)
    {
        var rawMessage = request.Message?.Trim() ?? string.Empty;
        var lower = rawMessage.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(lower))
        {
            return BuildGreeting(request.UserName);
        }

        // 1. Order Tracking by specific ID (e.g., "order #1", "track 10", "order 2")
        var orderIdMatch = Regex.Match(lower, @"(?:order|track|id|#)\s*#?\s*(\d+)");
        if (orderIdMatch.Success && int.TryParse(orderIdMatch.Groups[1].Value, out var orderId))
        {
            return await HandleOrderLookupByIdAsync(orderId);
        }

        // 2. User's Recent Orders inquiry ("my orders", "where is my order", "order status")
        if (lower.Contains("my order") || lower.Contains("recent order") || lower.Contains("order status") || lower == "orders")
        {
            return await HandleUserOrdersAsync(request.UserId);
        }

        // 3. Stock & Availability inquiries ("in stock", "available", "how many left")
        if (lower.Contains("stock") || lower.Contains("available") || lower.Contains("availability") || lower.Contains("how many"))
        {
            return await HandleStockInquiryAsync(lower);
        }

        // 4. Product Catalog & Recommendation inquiries
        if (lower.Contains("product") || lower.Contains("recommend") || lower.Contains("suggest") || 
            lower.Contains("headphone") || lower.Contains("keyboard") || lower.Contains("monitor") ||
            lower.Contains("electronic") || lower.Contains("accessor") || lower.Contains("catalog") ||
            lower.Contains("buy") || lower.Contains("shop") || lower.Contains("price") || lower.Contains("cost"))
        {
            return await HandleProductSearchAsync(lower);
        }

        // 5. Shipping & Delivery FAQ
        if (lower.Contains("ship") || lower.Contains("deliver") || lower.Contains("tracking") || lower.Contains("when will it arrive"))
        {
            return new ChatMessageResponse
            {
                Intent = "FAQ_Shipping",
                Reply = "📦 **Shipping & Delivery Policy**:\n\n" +
                        "• **Standard Delivery**: 3 to 5 business days across the country ($5.99 flat or free over $50).\n" +
                        "• **Express Delivery**: 1 to 2 business days ($14.99 or FREE for orders over $150).\n" +
                        "• **Tracking**: Every order includes live tracking updates. You can track any order right here by typing `track #<id>`!",
                Suggestions = new List<string> { "Track an Order", "Browse Products", "Return Policy", "Contact Support" }
            };
        }

        // 6. Return & Refund Policy FAQ
        if (lower.Contains("return") || lower.Contains("refund") || lower.Contains("warranty") || lower.Contains("exchange"))
        {
            return new ChatMessageResponse
            {
                Intent = "FAQ_Returns",
                Reply = "🔄 **Hassle-Free 30-Day Returns**:\n\n" +
                        "• You can return any product within **30 days** of delivery in its original packaging.\n" +
                        "• Prepaid return labels are provided automatically for eligible returns.\n" +
                        "• Refunds are issued back to your original payment method within 3 to 5 business days after inspection.",
                Suggestions = new List<string> { "Shipping Policy", "Browse Products", "Contact Support" }
            };
        }

        // 7. Payment & Checkout FAQ
        if (lower.Contains("payment") || lower.Contains("pay") || lower.Contains("card") || lower.Contains("checkout") || lower.Contains("visa"))
        {
            return new ChatMessageResponse
            {
                Intent = "FAQ_Payment",
                Reply = "💳 **Accepted Payment Methods & Security**:\n\n" +
                        "• We accept Visa, MasterCard, American Express, and Discover.\n" +
                        "• All transactions are secured using industry-standard AES-256 and SSL encryption.\n" +
                        "• To checkout, simply click your cart in the top navigation and click **Proceed to Checkout**.",
                Suggestions = new List<string> { "Browse Products", "Shipping Policy", "Contact Support" }
            };
        }

        // 8. Contact & Human Agent Escalation
        if (lower.Contains("contact") || lower.Contains("agent") || lower.Contains("human") || lower.Contains("support") || lower.Contains("help") || lower.Contains("email"))
        {
            return new ChatMessageResponse
            {
                Intent = "FAQ_Contact",
                Reply = "📞 **Customer Care & Live Escalation**:\n\n" +
                        "Our support specialists are here for you 24/7:\n" +
                        "• **Email**: support@apexstore.com\n" +
                        "• **Phone**: 1-800-APEX-STORE (Mon-Fri, 9am - 8pm EST)\n" +
                        "• **Priority Assistance**: Administrators and logged-in customers can also initiate direct order reviews from their portal.",
                Suggestions = new List<string> { "Track an Order", "Browse Products", "Return Policy" }
            };
        }

        // 9. Greetings
        if (lower.Contains("hi") || lower.Contains("hello") || lower.Contains("hey") || lower.Contains("good morning") || lower.Contains("good evening") || lower.Contains("who are you"))
        {
            return BuildGreeting(request.UserName);
        }

        // 10. Fallback / Default intelligent response
        return new ChatMessageResponse
        {
            Intent = "Fallback",
            Reply = "I'm here to help! You can ask me to **track an order**, **search for products**, check **stock availability**, or get details on our **shipping and return policies**.",
            Suggestions = new List<string>
            {
                "Track My Order",
                "Show Electronics",
                "Check Stock",
                "Shipping & Returns",
                "Contact Support"
            }
        };
    }

    private ChatMessageResponse BuildGreeting(string? userName)
    {
        var greetingName = !string.IsNullOrWhiteSpace(userName) ? $" {userName}" : "";
        return new ChatMessageResponse
        {
            Intent = "Greeting",
            Reply = $"Hello{greetingName}! 👋 I'm **ApexBot**, your virtual e-commerce customer assistant.\n\nHow can I help you today? Feel free to ask about your orders, product recommendations, stock levels, or store policies.",
            Suggestions = new List<string>
            {
                "Track My Order",
                "Browse Best Sellers",
                "Shipping Policy",
                "Return Policy",
                "Contact Support"
            }
        };
    }

    private async Task<ChatMessageResponse> HandleOrderLookupByIdAsync(int orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            return new ChatMessageResponse
            {
                Intent = "Order_NotFound",
                Reply = $"⚠️ Sorry, I could not find **Order #{orderId}** in our system. Please double-check your order number or check your order history in the Orders page.",
                Suggestions = new List<string> { "Check My Orders", "Browse Products", "Contact Support" }
            };
        }

        string statusExplanation = order.Status switch
        {
            "Pending" => "⏳ Your order is currently **Pending**. It has been recorded and is awaiting checkout confirmation.",
            "CheckedOut" => "📋 Your order is **Checked Out** and awaiting payment authorization.",
            "Paid" => "✅ Your order is **Paid & Confirmed**! Our warehouse team is currently preparing it for dispatch.",
            _ => $"Your order status is **{order.Status}**."
        };

        var orderCard = new ChatOrderCard
        {
            Id = order.Id,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            ItemCount = order.OrderItems?.Count ?? 0
        };

        return new ChatMessageResponse
        {
            Intent = "Order_Found",
            Reply = $"Here are the details for **Order #{order.Id}**:\n\n{statusExplanation}\n\n• **Total Amount**: ${order.TotalAmount:F2}\n• **Items Count**: {orderCard.ItemCount}\n• **Order Placed**: {order.CreatedAt:MMM dd, yyyy}",
            Order = orderCard,
            Suggestions = new List<string> { "Check Another Order", "Shipping Info", "Browse Products" }
        };
    }

    private async Task<ChatMessageResponse> HandleUserOrdersAsync(int? userId)
    {
        if (!userId.HasValue || userId.Value <= 0)
        {
            return new ChatMessageResponse
            {
                Intent = "Order_NeedLogin",
                Reply = "To see your order history, please log in with your customer account, or provide your specific **Order #** (for example: `track #1`).",
                Suggestions = new List<string> { "Track #1", "Track #2", "Browse Products" }
            };
        }

        var orders = await _orderRepository.GetByUserIdAsync(userId.Value);
        if (orders == null || orders.Count == 0)
        {
            return new ChatMessageResponse
            {
                Intent = "Order_NoUserOrders",
                Reply = "You haven't placed any orders yet. Explore our curated catalog and enjoy fast, free express shipping on orders over $150!",
                Suggestions = new List<string> { "Browse Products", "Shipping Policy" }
            };
        }

        var latest = orders.OrderByDescending(o => o.CreatedAt).First();
        var orderCard = new ChatOrderCard
        {
            Id = latest.Id,
            Status = latest.Status,
            TotalAmount = latest.TotalAmount,
            CreatedAt = latest.CreatedAt,
            ItemCount = latest.OrderItems?.Count ?? 0
        };

        return new ChatMessageResponse
        {
            Intent = "Order_UserLatest",
            Reply = $"You have placed **{orders.Count}** order(s). Your most recent is **Order #{latest.Id}** (Status: **{latest.Status}**, Total: **${latest.TotalAmount:F2}**).",
            Order = orderCard,
            Suggestions = new List<string> { $"Track #{latest.Id}", "View All Orders", "Browse Products" }
        };
    }

    private async Task<ChatMessageResponse> HandleStockInquiryAsync(string query)
    {
        var allProducts = await _productCatalogClient.GetAllProductsAsync();
        
        // Find product mentioned in query
        var match = allProducts.FirstOrDefault(p => 
            query.Contains(p.Name.ToLowerInvariant()) || 
            p.Name.ToLowerInvariant().Split(' ').Any(w => w.Length > 3 && query.Contains(w)));

        if (match != null)
        {
            var productCard = new ChatProductCard
            {
                Id = match.Id,
                Name = match.Name,
                Description = match.Description,
                Price = match.Price,
                Category = match.Category,
                Quantity = match.Quantity
            };

            var inStockText = match.Quantity > 0 
                ? $"✅ **In Stock**: We have **{match.Quantity}** unit(s) available for immediate shipment."
                : "❌ **Out of Stock**: This item is currently unavailable.";

            return new ChatMessageResponse
            {
                Intent = "Stock_Found",
                Reply = $"**{match.Name}** (${match.Price:F2})\n\n{inStockText}",
                Products = new List<ChatProductCard> { productCard },
                Suggestions = new List<string> { "Browse More Products", "Shipping Times", "Return Policy" }
            };
        }

        return new ChatMessageResponse
        {
            Intent = "Stock_Generic",
            Reply = "Which product would you like to check stock for? Here are some of our popular in-stock items:",
            Products = allProducts.Take(3).Select(p => new ChatProductCard
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Category = p.Category,
                Quantity = p.Quantity
            }).ToList(),
            Suggestions = new List<string> { "Headphones Stock", "Keyboard Stock", "Monitor Stock" }
        };
    }

    private async Task<ChatMessageResponse> HandleProductSearchAsync(string query)
    {
        var allProducts = await _productCatalogClient.GetAllProductsAsync();
        
        // Filter by relevance
        var matches = allProducts.Where(p => 
            query.Contains(p.Name.ToLowerInvariant()) ||
            query.Contains(p.Category.ToLowerInvariant()) ||
            p.Name.ToLowerInvariant().Split(' ').Any(w => w.Length > 3 && query.Contains(w)) ||
            p.Category.ToLowerInvariant().Split(' ').Any(w => w.Length > 3 && query.Contains(w))
        ).ToList();

        if (matches.Count == 0)
        {
            matches = allProducts.Take(3).ToList();
        }

        var productCards = matches.Take(3).Select(p => new ChatProductCard
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Category = p.Category,
            Quantity = p.Quantity
        }).ToList();

        return new ChatMessageResponse
        {
            Intent = "Product_Recommendations",
            Reply = $"Here are the items matching your search. You can add them directly to your cart or view full specifications:",
            Products = productCards,
            Suggestions = new List<string> { "Shipping Policy", "Track an Order", "Checkout Help" }
        };
    }
}
