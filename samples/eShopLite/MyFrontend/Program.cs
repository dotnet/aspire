using GrpcBasket;
using MyFrontend.Components;
using MyFrontend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpForwarderWithServiceDiscovery();

builder.Services.AddHttpClient<CatalogServiceClient>(c => c.BaseAddress = new("https://catalogservice"));

builder.Services.AddSingleton<BasketServiceClient>()
                .AddGrpcClient<Basket.BasketClient>(o => o.Address = new("https://basketservice"));

builder.Services.AddRazorComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
}

app.UseStaticFiles();

app.UseAntiforgery();

app.MapRazorComponents<App>();

app.MapForwarder("/catalog/images/{id}", "https://catalogservice", "/api/v1/catalog/items/{id}/image");

app.MapDefaultEndpoints();

app.Run();
