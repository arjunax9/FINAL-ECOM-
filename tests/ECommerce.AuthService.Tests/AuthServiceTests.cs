using System.IdentityModel.Tokens.Jwt;
using ECommerce.AuthService.Repositories;
using ECommerce.AuthService.Services;
using ECommerce.Models.Entities;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace ECommerce.AuthService.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly IConfiguration _config;
    private readonly Services.AuthService _authService;

    public AuthServiceTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:Key", "ThisIsMyVerySimpleSecretKeyForECommerceProject12345" },
            { "Jwt:Issuer", "ECommerceAPI" },
            { "Jwt:Audience", "ECommerceUsers" }
        };

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _authService = new Services.AuthService(_mockUserRepo.Object, _config);
    }

    [Fact]
    public async Task RegisterAsync_ValidNewUser_HashesPasswordAndAssignsCustomerRole()
    {
        // Arrange
        var user = new User
        {
            Name = "John Doe",
            Email = "john@example.com",
            Password = "PlainTextPassword123"
        };

        _mockUserRepo.Setup(r => r.GetByEmailAsync("john@example.com"))
            .ReturnsAsync((User?)null);

        _mockUserRepo.Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        var result = await _authService.RegisterAsync(user);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Customer", result.Role);
        Assert.NotEqual("PlainTextPassword123", result.Password);
        Assert.True(PasswordHasher.Verify(result.Password!, "PlainTextPassword123"));
        _mockUserRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var existing = new User { Email = "duplicate@example.com" };
        _mockUserRepo.Setup(r => r.GetByEmailAsync("duplicate@example.com"))
            .ReturnsAsync(existing);

        var newUser = new User
        {
            Email = "duplicate@example.com",
            Password = "Password123"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.RegisterAsync(newUser));
        _mockUserRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSignedJwtToken()
    {
        // Arrange
        var password = "CorrectPassword123!";
        var user = new User
        {
            Id = 42,
            Name = "Alice Admin",
            Email = "alice@example.com",
            Password = PasswordHasher.Hash(password),
            Role = "Admin"
        };

        _mockUserRepo.Setup(r => r.GetByEmailAsync("alice@example.com"))
            .ReturnsAsync(user);

        // Act
        var token = await _authService.LoginAsync("alice@example.com", password);

        // Assert
        Assert.NotNull(token);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        Assert.Equal("ECommerceAPI", jwt.Issuer);
        Assert.Contains(jwt.Audiences, a => a == "ECommerceUsers");
        Assert.Contains(jwt.Claims, c => c.Type == "role" && c.Value == "Admin");
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "alice@example.com");
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsNull()
    {
        // Arrange
        var user = new User
        {
            Email = "user@example.com",
            Password = PasswordHasher.Hash("Secret123"),
            Role = "Customer"
        };

        _mockUserRepo.Setup(r => r.GetByEmailAsync("user@example.com"))
            .ReturnsAsync(user);

        // Act
        var token = await _authService.LoginAsync("user@example.com", "WrongPassword");

        // Assert
        Assert.Null(token);
    }

    [Fact]
    public async Task LoginAsync_NonExistentEmail_ReturnsNull()
    {
        // Arrange
        _mockUserRepo.Setup(r => r.GetByEmailAsync("unknown@example.com"))
            .ReturnsAsync((User?)null);

        // Act
        var token = await _authService.LoginAsync("unknown@example.com", "Password123");

        // Assert
        Assert.Null(token);
    }
}
