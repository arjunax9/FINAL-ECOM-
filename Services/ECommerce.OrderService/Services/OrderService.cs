using ECommerce.Models.DTOs;
using ECommerce.Models.Entities;
using ECommerce.OrderService.Clients;
using ECommerce.OrderService.Hubs;
using ECommerce.OrderService.Repositories;
using Microsoft.AspNetCore.SignalR;

namespace ECommerce.OrderService.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductCatalogClient _productClient;
    private readonly IPaymentService _paymentService;
    private readonly IHubContext<OrderHub> _hubContext;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        IProductCatalogClient productClient,
        IPaymentService paymentService,
        IHubContext<OrderHub> hubContext,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _productClient = productClient;
        _paymentService = paymentService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        var order = new Order
        {
            UserId = request.UserId,
            Status = "Pending",
            OrderItems = new List<OrderItem>()
        };

        decimal total = 0m;

        foreach (var item in request.Items)
        {
            // Inter-service HTTP call to Product microservice (Port 5002)
            var product = await _productClient.GetProductByIdAsync(item.ProductId);
            if (product == null)
            {
                throw new InvalidOperationException($"Product {item.ProductId} not found in Product Catalog Service.");
            }

            if (product.Quantity < item.Quantity)
            {
                throw new InvalidOperationException($"Insufficient stock for product '{product.Name}'. Requested {item.Quantity}, available {product.Quantity}.");
            }

            // Deduct stock via Product Microservice HTTP endpoint
            var deducted = await _productClient.DeductStockAsync(item.ProductId, item.Quantity);
            if (!deducted)
            {
                throw new InvalidOperationException($"Failed to reserve stock for product '{product.Name}'.");
            }

            total += product.Price * item.Quantity;

            order.OrderItems.Add(new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Price = product.Price
            });
        }

        order.TotalAmount = total;
        var created = await _orderRepository.AddAsync(order);

        await BroadcastOrderUpdate(created.Id, created.Status);
        return created;
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _orderRepository.GetByIdAsync(id);
    }

    public async Task<List<Order>> GetByUserIdAsync(int userId)
    {
        return await _orderRepository.GetByUserIdAsync(userId);
    }

    public async Task<List<Order>> GetAllAsync()
    {
        return await _orderRepository.GetAllAsync();
    }

    public async Task CheckoutAsync(int id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null) throw new InvalidOperationException("Order not found");

        order.Status = "CheckedOut";
        await _orderRepository.UpdateAsync(order);
        await BroadcastOrderUpdate(order.Id, order.Status);
    }

    public async Task<bool> ProcessPaymentAsync(int id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null) throw new InvalidOperationException("Order not found");

        var success = await _paymentService.ProcessPaymentAsync(order.TotalAmount);
        if (success)
        {
            order.Status = "Paid";
            await _orderRepository.UpdateAsync(order);
            await BroadcastOrderUpdate(order.Id, order.Status);
        }

        return success;
    }

    public async Task<Order> ApproveOrderAsync(int id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null) throw new InvalidOperationException("Order not found");

        order.Status = "Paid";
        await _orderRepository.UpdateAsync(order);
        await BroadcastOrderUpdate(order.Id, order.Status);
        return order;
    }

    private async Task BroadcastOrderUpdate(int orderId, string status)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("OrderUpdated", orderId, status);
            await _hubContext.Clients.Group($"order_{orderId}").SendAsync("OrderUpdated", orderId, status);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast SignalR update for order {OrderId}", orderId);
        }
    }
}
