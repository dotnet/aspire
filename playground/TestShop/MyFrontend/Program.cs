using GrpcBasket;
using MyFrontend.Components;
using MyFrontend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpForwarderWithServiceDiscovery();

builder.Services.AddHttpClient<CatalogServiceClient>(c => c.BaseAddress = new("https+http://catalogservice"));

var isHttps = Environment.GetEnvironmentVariable("DOTNET_LAUNCHPROFILE") == "https";

builder.Services.AddSingleton<BasketServiceClient>()
                .AddGrpcClient<Basket.BasketClient>(o => o.Address = new($"{(isHttps ? "https" : "http")}://basketservice"));

builder.Services.AddRazorComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAntiforgery();

app.MapRazorComponents<App>();

app.MapForwarder("/catalog/images/{id}", "https+http://catalogservice", "/api/v1/catalog/items/{id}/image");

app.MapDefaultEndpoints();

app.Run();
