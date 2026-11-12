using System.Security.Claims;
using ECommerce.AuthService.Controllers;
using ECommerce.AuthService.Services;
using ECommerce.Models.DTOs;
using ECommerce.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ECommerce.AuthService.Tests;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _controller = new AuthController(_mockAuthService.Object);

        // Default ControllerContext (Anonymous user)
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task Register_ValidCustomer_ReturnsOk()
    {
        // Arrange
        var user = new User { Name = "Jane", Email = "jane@example.com", Password = "Pass" };
        var created = new User { Id = 1, Name = "Jane", Email = "jane@example.com", Role = "Customer" };

        _mockAuthService.Setup(s => s.RegisterAsync(It.IsAny<User>()))
            .ReturnsAsync(created);

        // Act
        var result = await _controller.Register(user);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var user = new User { Email = "existing@example.com" };
        _mockAuthService.Setup(s => s.RegisterAsync(It.IsAny<User>()))
            .ThrowsAsync(new InvalidOperationException("User with this email already exists."));

        // Act
        var result = await _controller.Register(user);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task Register_CustomerAttemptingAdminElevation_ReturnsForbid()
    {
        // Arrange
        var user = new User { Name = "Hacker", Email = "hacker@example.com", Role = "Admin" };

        // Act (User is not in "Admin" role)
        var result = await _controller.Register(user);

        // Assert
        Assert.IsType<ForbidResult>(result);
        _mockAuthService.Verify(s => s.RegisterAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var request = new LoginRequest { Email = "user@test.com", Password = "ValidPassword" };
        _mockAuthService.Setup(s => s.LoginAsync("user@test.com", "ValidPassword"))
            .ReturnsAsync("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.dummytoken");

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest { Email = "user@test.com", Password = "WrongPassword" };
        _mockAuthService.Setup(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
