using ECommerce.AuthService.Services;
using Xunit;

namespace ECommerce.AuthService.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_GeneratesValidHashStringWithThreeDotSeparatedParts()
    {
        // Act
        var hash = PasswordHasher.Hash("SecureP@ssw0rd123");

        // Assert
        Assert.NotNull(hash);
        var parts = hash.Split('.');
        Assert.Equal(3, parts.Length);
        Assert.Equal("10000", parts[0]); // Default iterations
        Assert.NotEmpty(parts[1]);       // Salt (Base64)
        Assert.NotEmpty(parts[2]);       // Hash key (Base64)
    }

    [Fact]
    public void Hash_ProducesDifferentSaltAndHashForSamePassword()
    {
        // Act
        var hash1 = PasswordHasher.Hash("MyPassword");
        var hash2 = PasswordHasher.Hash("MyPassword");

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Theory]
    [InlineData("Admin123!")]
    [InlineData("Password123")]
    [InlineData("Complex!@#$%^&*()_+~`|}{[]:;?><,./-=")]
    public void Verify_ReturnsTrueForCorrectPassword(string password)
    {
        // Arrange
        var hash = PasswordHasher.Hash(password);

        // Act
        var result = PasswordHasher.Verify(hash, password);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Verify_ReturnsFalseForWrongPassword()
    {
        // Arrange
        var hash = PasswordHasher.Hash("CorrectPassword");

        // Act
        var result = PasswordHasher.Verify(hash, "IncorrectPassword");

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-hash")]
    [InlineData("1000.onlytwoparts")]
    public void Verify_ReturnsFalseForMalformedHash(string malformedHash)
    {
        // Act
        var result = PasswordHasher.Verify(malformedHash, "AnyPassword");

        // Assert
        Assert.False(result);
    }
}
