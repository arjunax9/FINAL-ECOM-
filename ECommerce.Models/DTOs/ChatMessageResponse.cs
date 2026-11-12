namespace ECommerce.Models.DTOs;

public class ChatProductCard
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public bool InStock => Quantity > 0;
}

public class ChatOrderCard
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
}

public class ChatMessageResponse
{
    public string Reply { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public List<string> Suggestions { get; set; } = new();
    public List<ChatProductCard> Products { get; set; } = new();
    public ChatOrderCard? Order { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
