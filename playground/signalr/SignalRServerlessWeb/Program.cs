using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

var serviceManager = new ServiceManagerBuilder()
        .WithOptions(option =>
        {
            option.ConnectionString = builder.Configuration.GetConnectionString("signalrServerless");
        })
        .BuildServiceManager();
var hubName = "notificationHub";
var hubContext = await serviceManager.CreateHubContextAsync(hubName, default);
builder.Services.AddHostedService(sp => new PeriodicBroadcaster(hubContext));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};
app.MapPost($"{hubName}/negotiate", async (string? userId) =>
{
    var negotiateResponse = await hubContext.NegotiateAsync(new NegotiationOptions
    {
        UserId = userId ?? "user1"
    });

    return Results.Json(negotiateResponse, jsonSerializerOptions);
});

app.MapRazorPages();
app.Run();

internal sealed class PeriodicBroadcaster(ServiceHubContext hubContext) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var count = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            await hubContext.Clients.All.SendAsync("newMessage", $"Current count is: {count++}", stoppingToken);
            await Task.Delay(2000, stoppingToken);
        }
    }
}
