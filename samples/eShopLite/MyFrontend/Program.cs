using GrpcBasket;
using MyFrontend.Components;
using MyFrontend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpForwarderWithServiceDiscovery();

builder.Services.AddHttpClient<CatalogServiceClient>(c => c.BaseAddress = new("http://catalogservice"));

builder.Services.AddSingleton<BasketServiceClient>()
                .AddGrpcClient<Basket.BasketClient>(o => o.Address = new("http://basketservice"));

builder.Services.AddRazorComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
}

app.UseStaticFiles();

app.UseAntiforgery();

app.MapRazorComponents<App>();

app.MapGet("/admin", (IConfiguration config) =>
{
    return config["GRAFANA_URL"] switch
    {
        string url => Results.Redirect(url),
        null => Results.StatusCode(500)
    };
});

app.MapForwarder("/catalog/images/{id}", "http://catalogservice", "/api/v1/catalog/items/{id}/image");

app.MapDefaultEndpoints();

//_ = Task.Run(async () =>
//{
//    Meter m = new Meter("Microsoft.AspNetCore.Hosting");
//    var s = m.CreateHistogram<double>("http.server.request.duration", "s", "My test histogram.");

//    while (true)
//    {
//        await Task.Delay(5000);
//        s.Record(0.5, new KeyValuePair<string, object?>("url.scheme", "http"));
//    }
//});

app.Run();
