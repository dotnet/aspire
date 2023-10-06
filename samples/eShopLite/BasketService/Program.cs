using BasketService.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddGrpc();

builder.AddRedis("basketCache");
builder.Services.AddTransient<IBasketRepository, RedisBasketRepository>();

// When running for Development, don't fail at startup if the developer hasn't configured ServiceBus yet.
if (!builder.Environment.IsDevelopment() || builder.Configuration[AspireServiceBusExtensions.DefaultNamespaceConfigKey] is not null)
{
    builder.AddAzureServiceBus("messaging");
}

var app = builder.Build();

app.MapGrpcService<BasketService.BasketService>();
app.MapDefaultEndpoints();

app.Run();
