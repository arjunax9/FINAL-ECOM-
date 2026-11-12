var builder = WebApplication.CreateBuilder(args);

// Add YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseRouting();

// Health/Status endpoint
app.MapGet("/", () => Results.Ok(new
{
    Gateway = "E-Commerce API Gateway (YARP)",
    Status = "Healthy",
    Port = 5000,
    Routes = new[]
    {
        new { Route = "/api/auth/*", Target = "Auth Service (Port 5001)" },
        new { Route = "/api/users/*", Target = "Auth Service (Port 5001)" },
        new { Route = "/api/products/*", Target = "Product Service (Port 5002)" },
        new { Route = "/api/inventory/*", Target = "Product Service (Port 5002)" },
        new { Route = "/api/orders/*", Target = "Order Service (Port 5003)" },
        new { Route = "/api/chat/*", Target = "Customer Support Chatbot (Port 5003)" },
        new { Route = "/orderHub/*", Target = "Order Service SignalR Hub (Port 5003)" }
    }
}));

app.MapReverseProxy();

app.Run();
