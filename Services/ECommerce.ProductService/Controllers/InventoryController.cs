using ECommerce.ProductService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    /// <summary>
    /// Admin only: Check specific product inventory stock
    /// </summary>
    [HttpGet("{productId}")]
    public async Task<IActionResult> Get(int productId)
    {
        var qty = await _inventoryService.GetQuantityAsync(productId);
        return Ok(new { productId, quantity = qty });
    }

    /// <summary>
    /// Admin only: Update stock quantity
    /// </summary>
    [HttpPut("{productId}")]
    public async Task<IActionResult> Update(int productId, [FromBody] int quantity)
    {
        await _inventoryService.UpdateQuantityAsync(productId, quantity);
        return Ok(new { productId, quantity });
    }

    /// <summary>
    /// Internal microservice endpoint: Deduct stock during order checkout
    /// </summary>
    [HttpPost("{productId}/deduct")]
    [AllowAnonymous]
    public async Task<IActionResult> DeductStock(int productId, [FromBody] int quantity)
    {
        var success = await _inventoryService.DeductStockAsync(productId, quantity);
        if (!success)
        {
            return BadRequest(new { error = $"Insufficient stock for product {productId} or product not found." });
        }

        return Ok(new { success = true, productId, deducted = quantity });
    }
}
