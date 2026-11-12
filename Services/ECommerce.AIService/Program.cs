using ECommerce.AIService.Clients;
using ECommerce.AIService.Configuration;
using ECommerce.AIService.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add Configuration Options
builder.Services.Configure<AzureAiOptions>(builder.Configuration.GetSection(AzureAiOptions.SectionName));
builder.Services.Configure<ServiceUrlsOptions>(builder.Configuration.GetSection(ServiceUrlsOptions.SectionName));

// Configure HttpClients
builder.Services.AddHttpClient<IAzureAiClient, AzureAiClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IProductServiceClient, ProductServiceClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<IOrderServiceClient, OrderServiceClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Configure AI Services & Agents
builder.Services.AddSingleton<IGovernanceService, GovernanceService>();
builder.Services.AddSingleton<IRagService, RagService>();
builder.Services.AddScoped<ICustomerServiceAgent, CustomerServiceAgent>();
builder.Services.AddScoped<IProductRecommendationAgent, ProductRecommendationAgent>();
builder.Services.AddScoped<IAiAgentOrchestrator, AiAgentOrchestrator>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ECommerce AI Service API (Microsoft AI Foundry)",
        Version = "v1",
        Description = "Microservice powered by Microsoft AI Foundry (gpt-4.1-mini & text-embedding-3-small) providing autonomous customer service agents, semantic product recommendations, enterprise RAG, and responsible AI governance."
    });
});

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

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ECommerce AI Service v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// Eagerly initialize RAG knowledge base embeddings in the background on startup
_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(3000);
        using var scope = app.Services.CreateScope();
        var ragService = scope.ServiceProvider.GetRequiredService<IRagService>();
        await ragService.InitializeIndexAsync();
        app.Logger.LogInformation("Enterprise RAG Knowledge Base successfully initialized with {Count} documents", ragService.TotalIndexedDocuments);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Background RAG initialization encountered an error");
    }
});

app.Run();
