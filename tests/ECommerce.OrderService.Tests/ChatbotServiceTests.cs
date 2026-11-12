using ECommerce.Models.DTOs;
using ECommerce.Models.Entities;
using ECommerce.OrderService.Clients;
using ECommerce.OrderService.Repositories;
using ECommerce.OrderService.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ECommerce.OrderService.Tests;

public class ChatbotServiceTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepo;
    private readonly Mock<IProductCatalogClient> _mockProductClient;
    private readonly Mock<ILogger<ChatbotService>> _mockLogger;
    private readonly ChatbotService _chatbotService;

    public ChatbotServiceTests()
    {
        _mockOrderRepo = new Mock<IOrderRepository>();
        _mockProductClient = new Mock<IProductCatalogClient>();
        _mockLogger = new Mock<ILogger<ChatbotService>>();

        _chatbotService = new ChatbotService(
            _mockOrderRepo.Object,
            _mockProductClient.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessMessageAsync_EmptyMessage_ReturnsGreeting()
    {
        // Act
        var result = await _chatbotService.ProcessMessageAsync(new ChatMessageRequest { Message = "" });

        // Assert
        Assert.Equal("Greeting", result.Intent);
        Assert.Contains("ApexBot", result.Reply);
        Assert.NotEmpty(result.Suggestions);
    }

    [Fact]
    public async Task ProcessMessageAsync_GreetingWithUserName_PersonalizesReply()
    {
        // Act
        var result = await _chatbotService.ProcessMessageAsync(new ChatMessageRequest
        {
            Message = "Hello there",
            UserName = "Sarah Connor"
        });

        // Assert
        Assert.Equal("Greeting", result.Intent);
        Assert.Contains("Sarah Connor", result.Reply);
    }

    [Fact]
    public async Task ProcessMessageAsync_OrderLookupExisting_ReturnsOrderFoundWithCard()
    {
        // Arrange
        var order = new Order
        {
            Id = 42,
            Status = "Paid",
            TotalAmount = 299.99m,
            CreatedAt = DateTime.UtcNow,
            OrderItems = new List<OrderItem> { new OrderItem { ProductId = 1, Quantity = 2 } }
        };
        _mockOrderRepo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(order);

        // Act
        var result = await _chatbotService.ProcessMessageAsync(new ChatMessageRequest
        {
            Message = "Please track order #42"
        });

        // Assert
        Assert.Equal("Order_Found", result.Intent);
        Assert.NotNull(result.Order);
        Assert.Equal(42, result.Order.Id);
        Assert.Equal("Paid", result.Order.Status);
        Assert.Equal(299.99m, result.Order.TotalAmount);
        Assert.Contains("Paid & Confirmed", result.Reply);
    }

    [Fact]
    public async Task ProcessMessageAsync_OrderLookupNonExistent_ReturnsOrderNotFound()
    {
        // Arrange
        _mockOrderRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Order?)null);

        // Act
        var result = await _chatbotService.ProcessMessageAsync(new ChatMessageRequest
        {
            Message = "Where is order 999?"
        });

        // Assert
        Assert.Equal("Order_NotFound", result.Intent);
        Assert.Null(result.Order);
        Assert.Contains("could not find", result.Reply);
    }

    [Fact]
    public async Task ProcessMessageAsync_UserOrdersLoggedIn_ReturnsLatestOrder()
    {
        // Arrange
        var orders = new List<Order>
        {
            new Order { Id = 1, UserId = 7, Status = "CheckedOut", TotalAmount = 50m, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new Order { Id = 2, UserId = 7, Status = "Paid", TotalAmount = 150m, CreatedAt = DateTime.UtcNow }
        };
        _mockOrderRepo.Setup(r => r.GetByUserIdAsync(7)).ReturnsAsync(orders);

        // Act
        var result = await _chatbotService.ProcessMessageAsync(new ChatMessageRequest
        {
            Message = "Show my orders",
            UserId = 7
        });

        // Assert
        Assert.Equal("Order_UserLatest", result.Intent);
        Assert.NotNull(result.Order);
        Assert.Equal(2, result.Order.Id); // Latest order
        Assert.Contains("**2** order(s)", result.Reply);
    }

    [Fact]
    public async Task ProcessMessageAsync_UserOrdersAnonymous_PromptsLogin()
    {
        // Act
        var result = await _chatbotService.ProcessMessageAsync(new ChatMessageRequest
        {
            Message = "Where are my orders?"
        });

        // Assert
        Assert.Equal("Order_NeedLogin", result.Intent);
        Assert.Contains("log in", result.Reply);
    }

    [Fact]
    public async Task ProcessMessageAsync_ProductSearch_ReturnsProductCards()
    {
        // Arrange
        var catalog = new List<ProductCatalogItem>
        {
            new ProductCatalogItem { Id = 1, Name = "Wireless Headphones", Price = 199.99m, Category = "Electronics", Quantity = 10 },
            new ProductCatalogItem { Id = 2, Name = "Mechanical Keyboard", Price = 89.99m, Category = "Accessories", Quantity = 25 }
        };
        _mockProductClient.Setup(c => c.GetAllProductsAsync()).ReturnsAsync(catalog);

        // Act
        var result = await _chatbotService.ProcessMessageAsync(new ChatMessageRequest
        {
            Message = "Can you recommend some headphones?"
        });

        // Assert
        Assert.Equal("Product_Recommendations", result.Intent);
        Assert.NotEmpty(result.Products);
        Assert.Contains(result.Products, p => p.Name.Contains("Headphones"));
    }

    [Fact]
    public async Task ProcessMessageAsync_StockInquiry_ReturnsStockLevel()
    {
        // Arrange
        var catalog = new List<ProductCatalogItem>
        {
            new ProductCatalogItem { Id = 3, Name = "Ultra HD 4K Monitor", Price = 349.50m, Category = "Electronics", Quantity = 15 }
        };
        _mockProductClient.Setup(c => c.GetAllProductsAsync()).ReturnsAsync(catalog);

        // Act
        var result = await _chatbotService.ProcessMessageAsync(new ChatMessageRequest
        {
            Message = "How much stock do you have for the 4k monitor?"
        });

        // Assert
        Assert.Equal("Stock_Found", result.Intent);
        Assert.Single(result.Products);
        Assert.Contains("**15** unit(s)", result.Reply);
    }

    [Theory]
    [InlineData("What is your shipping policy?", "FAQ_Shipping")]
    [InlineData("How do I return an item?", "FAQ_Returns")]
    [InlineData("What payment methods do you accept?", "FAQ_Payment")]
    [InlineData("Can I speak to an agent?", "FAQ_Contact")]
    public async Task ProcessMessageAsync_FAQQueries_ReturnCorrectIntent(string query, string expectedIntent)
    {
        // Act
        var result = await _chatbotService.ProcessMessageAsync(new ChatMessageRequest { Message = query });

        // Assert
        Assert.Equal(expectedIntent, result.Intent);
        Assert.NotEmpty(result.Reply);
        Assert.NotEmpty(result.Suggestions);
    }
}
