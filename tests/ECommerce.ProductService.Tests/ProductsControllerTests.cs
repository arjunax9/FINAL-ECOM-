using ECommerce.Models.Entities;
using ECommerce.ProductService.Controllers;
using ECommerce.ProductService.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ECommerce.ProductService.Tests;

public class ProductsControllerTests
{
    private readonly Mock<IProductService> _mockProductService;
    private readonly Mock<IInventoryService> _mockInventoryService;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _mockProductService = new Mock<IProductService>();
        _mockInventoryService = new Mock<IInventoryService>();
        _controller = new ProductsController(_mockProductService.Object, _mockInventoryService.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithProductsAndQuantities()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product { Id = 1, Name = "Keyboard", Price = 50.0m, Category = "Accessories" }
        };
        _mockProductService.Setup(s => s.GetAllAsync()).ReturnsAsync(products);
        _mockInventoryService.Setup(s => s.GetQuantityAsync(1)).ReturnsAsync(30);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetById_ProductExists_ReturnsOk()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "Monitor", Price = 200.0m };
        _mockProductService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(product);
        _mockInventoryService.Setup(s => s.GetQuantityAsync(1)).ReturnsAsync(15);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetById_ProductNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockProductService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((Product?)null);

        // Act
        var result = await _controller.GetById(99);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
