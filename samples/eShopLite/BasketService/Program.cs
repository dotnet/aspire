using BasketService.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddGrpc();

builder.AddRedis("basketcache");
builder.Services.AddTransient<IBasketRepository, RedisBasketRepository>();

builder.AddRabbitMQ("messaging");

var app = builder.Build();

app.MapGrpcService<BasketService.BasketService>();
app.MapDefaultEndpoints();

app.Run();
