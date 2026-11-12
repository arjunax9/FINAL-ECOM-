using ECommerce.Models.DTOs;
using ECommerce.OrderService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatbotService _chatbotService;

    public ChatController(IChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Request payload is required." });
        }

        var response = await _chatbotService.ProcessMessageAsync(request);
        return Ok(response);
    }
}
