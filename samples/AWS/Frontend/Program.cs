using Amazon.SQS;
using Frontend;
using Frontend.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOptions<AWSResources>()
                .Bind(builder.Configuration.GetSection("AWS").GetSection("Resources"));

builder.Services.AddSingleton<IAmazonSQS>(new AmazonSQSClient());

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
