using AspireStarterApplication._1.Web;
using AspireStarterApplication._1.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();
#if (UseRedisCache)
builder.AddRedisOutputCache("cache");
#endif

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

#if (!UseRedisCache)
builder.Services.AddOutputCache();

#endif
#if (HasHttpsProfile)
builder.Services.AddHttpClient<WeatherApiClient>(client => client.BaseAddress = new("https://apiservice"));
#else
builder.Services.AddHttpClient<WeatherApiClient>(client => client.BaseAddress = new("http://apiservice"));
#endif

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();

app.UseAntiforgery();

app.UseOutputCache();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
