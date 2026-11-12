namespace ECommerce.Models.DTOs;

public class ChatMessageRequest
{
    public string Message { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserRole { get; set; }
}
