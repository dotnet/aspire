using Microsoft.Azure.SignalR.Management;

var builder = WebApplication.CreateBuilder(args);

var serviceManager = new ServiceManagerBuilder()
        .WithOptions(option =>
        {
            option.ConnectionString = builder.Configuration["ConnectionStrings:signalrServerless"];
        })
        .BuildServiceManager();
var hubContext = await serviceManager.CreateHubContextAsync("myHubName", default);

builder.Services.AddRazorPages();
builder.Services.AddSingleton(hubContext).AddHostedService<PeriodicBroadcaster>();

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

app.MapPost("/negotiate", async (string? userId) =>
{
    var negotiateResponse = await hubContext.NegotiateAsync(new NegotiationOptions
    {
        UserId = userId ?? "user1"
    });

    return Results.Ok(negotiateResponse);
});

app.MapRazorPages();
app.Run();

internal class PeriodicBroadcaster(ServiceHubContext hubContext) : BackgroundService
{
    private int _count;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await hubContext.Clients.All.SendCoreAsync("newMessage", [$"Current count is: {_count++}"], stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
