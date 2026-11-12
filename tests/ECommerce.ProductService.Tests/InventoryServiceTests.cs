using ECommerce.Models.Entities;
using ECommerce.ProductService.Repositories;
using ECommerce.ProductService.Services;
using Moq;
using Xunit;

namespace ECommerce.ProductService.Tests;

public class InventoryServiceTests
{
    private readonly Mock<IInventoryRepository> _mockRepo;
    private readonly InventoryService _inventoryService;

    public InventoryServiceTests()
    {
        _mockRepo = new Mock<IInventoryRepository>();
        _inventoryService = new InventoryService(_mockRepo.Object);
    }

    [Fact]
    public async Task GetQuantityAsync_ExistingProduct_ReturnsStockQuantity()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByProductIdAsync(1))
            .ReturnsAsync(new Inventory { ProductId = 1, Quantity = 50 });

        // Act
        var qty = await _inventoryService.GetQuantityAsync(1);

        // Assert
        Assert.Equal(50, qty);
    }

    [Fact]
    public async Task GetQuantityAsync_MissingProduct_ReturnsZero()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByProductIdAsync(999))
            .ReturnsAsync((Inventory?)null);

        // Act
        var qty = await _inventoryService.GetQuantityAsync(999);

        // Assert
        Assert.Equal(0, qty);
    }

    [Fact]
    public async Task DeductStockAsync_SufficientStock_DeductsAndReturnsTrue()
    {
        // Arrange
        var inv = new Inventory { ProductId = 1, Quantity = 20 };
        _mockRepo.Setup(r => r.GetByProductIdAsync(1)).ReturnsAsync(inv);
        _mockRepo.Setup(r => r.UpdateAsync(inv)).Returns(Task.CompletedTask);

        // Act
        var success = await _inventoryService.DeductStockAsync(1, 5);

        // Assert
        Assert.True(success);
        Assert.Equal(15, inv.Quantity);
        _mockRepo.Verify(r => r.UpdateAsync(inv), Times.Once);
    }

    [Fact]
    public async Task DeductStockAsync_InsufficientStock_ReturnsFalseAndLeavesStockIntact()
    {
        // Arrange
        var inv = new Inventory { ProductId = 1, Quantity = 3 };
        _mockRepo.Setup(r => r.GetByProductIdAsync(1)).ReturnsAsync(inv);

        // Act
        var success = await _inventoryService.DeductStockAsync(1, 10);

        // Assert
        Assert.False(success);
        Assert.Equal(3, inv.Quantity);
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Inventory>()), Times.Never);
    }

    [Fact]
    public async Task DeductStockAsync_ProductNotFound_ReturnsFalse()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByProductIdAsync(404)).ReturnsAsync((Inventory?)null);

        // Act
        var success = await _inventoryService.DeductStockAsync(404, 1);

        // Assert
        Assert.False(success);
    }
}
