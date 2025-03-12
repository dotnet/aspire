using Microsoft.Azure.SignalR.Management;
using SignalRServerlessWeb;

var builder = WebApplication.CreateBuilder(args);

var serviceManager = new ServiceManagerBuilder()
        .WithOptions(option =>
        {
            option.ConnectionString = builder.Configuration["ConnectionStrings:signalrServerless"];
        })
        .BuildServiceManager();
var hubContext = await serviceManager.CreateHubContextAsync("myHubName", default);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton(hubContext).AddHostedService<BackgroundWorker>();

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
