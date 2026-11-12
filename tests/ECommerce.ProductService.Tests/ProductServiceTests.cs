using ECommerce.Models.Entities;
using ECommerce.ProductService.Repositories;
using ECommerce.ProductService.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace ECommerce.ProductService.Tests;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _mockRepo;
    private readonly IMemoryCache _memoryCache;
    private readonly Services.ProductService _productService;

    public ProductServiceTests()
    {
        _mockRepo = new Mock<IProductRepository>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _productService = new Services.ProductService(_mockRepo.Object, _memoryCache);
    }

    [Fact]
    public async Task GetAllAsync_CachesResult_SubsequentCallsDoNotHitRepository()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product { Id = 1, Name = "Laptop", Price = 999.99m, Category = "Electronics" },
            new Product { Id = 2, Name = "Mouse", Price = 29.99m, Category = "Accessories" }
        };
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(products);

        // Act
        var firstCall = await _productService.GetAllAsync();
        var secondCall = await _productService.GetAllAsync();

        // Assert
        Assert.Equal(2, firstCall.Count);
        Assert.Equal(2, secondCall.Count);
        _mockRepo.Verify(r => r.GetAllAsync(), Times.Once); // Cache hit on second call
    }

    [Fact]
    public async Task GetByIdAsync_ExistingProduct_ReturnsProductAndCaches()
    {
        // Arrange
        var product = new Product { Id = 10, Name = "Headphones", Price = 149.99m };
        _mockRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(product);

        // Act
        var result1 = await _productService.GetByIdAsync(10);
        var result2 = await _productService.GetByIdAsync(10);

        // Assert
        Assert.NotNull(result1);
        Assert.Equal("Headphones", result1.Name);
        _mockRepo.Verify(r => r.GetByIdAsync(10), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentProduct_ReturnsNull()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Product?)null);

        // Act
        var result = await _productService.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_AddsProductAndEvictsAllCache()
    {
        // Arrange
        var initialList = new List<Product> { new Product { Id = 1, Name = "Item 1" } };
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(initialList);

        // Populate cache
        await _productService.GetAllAsync();

        var newProduct = new Product { Id = 2, Name = "Item 2" };
        _mockRepo.Setup(r => r.AddAsync(newProduct)).ReturnsAsync(newProduct);

        // Act
        await _productService.AddAsync(newProduct);

        // Assert: Cache was cleared so GetAllAsync calls repo again
        var updatedList = new List<Product> { initialList[0], newProduct };
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(updatedList);
        var afterAdd = await _productService.GetAllAsync();

        Assert.Equal(2, afterAdd.Count);
        _mockRepo.Verify(r => r.GetAllAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task DeleteAsync_ExistingProduct_DeletesAndEvictsCache()
    {
        // Arrange
        var product = new Product { Id = 5, Name = "To Delete" };
        _mockRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(product);
        _mockRepo.Setup(r => r.DeleteAsync(product)).Returns(Task.CompletedTask);

        // Act
        await _productService.DeleteAsync(5);

        // Assert
        _mockRepo.Verify(r => r.DeleteAsync(product), Times.Once);
    }
}
