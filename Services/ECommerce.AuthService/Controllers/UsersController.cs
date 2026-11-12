using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ECommerce.AuthService.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public UsersController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userRepository.GetAllAsync();
        var safeUsers = users.Select(u => new
        {
            u.Id,
            u.Name,
            u.Email,
            u.Role
        });
        return Ok(safeUsers);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        return Ok(new { user.Id, user.Name, user.Email, user.Role });
    }

    [HttpPut("{id}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] string role)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        user.Role = role ?? "Customer";
        await _userRepository.UpdateAsync(user);

        return NoContent();
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var subClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(subClaim, out var id))
        {
            return Ok(new { name = User.Identity?.Name, roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value) });
        }

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        return Ok(new { user.Id, user.Name, user.Email, user.Role });
    }
}
