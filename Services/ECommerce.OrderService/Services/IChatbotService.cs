using ECommerce.Models.DTOs;

namespace ECommerce.OrderService.Services;

public interface IChatbotService
{
    Task<ChatMessageResponse> ProcessMessageAsync(ChatMessageRequest request);
}
