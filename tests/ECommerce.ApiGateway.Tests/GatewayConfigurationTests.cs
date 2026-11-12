using Microsoft.Extensions.Configuration;
using Xunit;

namespace ECommerce.ApiGateway.Tests;

public class GatewayConfigurationTests
{
    private readonly IConfiguration _config;

    public GatewayConfigurationTests()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Services", "ECommerce.ApiGateway", "appsettings.json");
        _config = new ConfigurationBuilder()
            .AddJsonFile(Path.GetFullPath(configPath), optional: false)
            .Build();
    }

    [Theory]
    [InlineData("auth-route", "auth-cluster", "/api/auth/{**catch-all}")]
    [InlineData("users-route", "auth-cluster", "/api/users/{**catch-all}")]
    [InlineData("products-route", "product-cluster", "/api/products/{**catch-all}")]
    [InlineData("inventory-route", "product-cluster", "/api/inventory/{**catch-all}")]
    [InlineData("orders-route", "order-cluster", "/api/orders/{**catch-all}")]
    [InlineData("ai-route", "ai-cluster", "/api/ai/{**catch-all}")]
    [InlineData("chat-route", "ai-cluster", "/api/chat/{**catch-all}")]
    [InlineData("signalr-route", "order-cluster", "/orderHub/{**catch-all}")]
    [InlineData("signalr-root-route", "order-cluster", "/orderHub")]
    public void ReverseProxy_Routes_ConfiguredCorrectly(string routeKey, string expectedCluster, string expectedPath)
    {
        var clusterId = _config[$"ReverseProxy:Routes:{routeKey}:ClusterId"];
        var path = _config[$"ReverseProxy:Routes:{routeKey}:Match:Path"];

        Assert.Equal(expectedCluster, clusterId);
        Assert.Equal(expectedPath, path);
    }

    [Theory]
    [InlineData("auth-cluster", "auth-destination")]
    [InlineData("product-cluster", "product-destination")]
    [InlineData("order-cluster", "order-destination")]
    [InlineData("ai-cluster", "ai-destination")]
    public void ReverseProxy_Clusters_HaveDestinationAddress(string clusterKey, string destinationKey)
    {
        var address = _config[$"ReverseProxy:Clusters:{clusterKey}:Destinations:{destinationKey}:Address"];
        Assert.NotNull(address);
        Assert.NotEmpty(address);
        Assert.StartsWith("http", address);
    }
}
