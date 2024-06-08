using Azure.Messaging.WebPubSub;

var builder = WebApplication.CreateBuilder(args);

builder.AddKeyedAzureWebPubSubServiceClient("wps1", "chatHub");
builder.AddKeyedAzureWebPubSubServiceClient("wps1", "notificationHub");

// add a background service to periodically broadcast messages to the client
builder.Services.AddHostedService<NotificationService>();

// Add services to the container.
builder.Services.AddRazorPages();

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

app.MapRazorPages();

// return the Client Access URL with negotiate endpoint
app.MapGet("/negotiate/chat", ([FromKeyedServices("chatHub")] WebPubSubServiceClient service) =>
{
    return
        new
        {
            url = service.GetClientAccessUri(roles: ["webpubsub.sendToGroup.group1", "webpubsub.joinLeaveGroup.group1"]).AbsoluteUri
        };
});

app.MapGet("/negotiate/notification", ([FromKeyedServices("notificationHub")] WebPubSubServiceClient service) =>
{
    return
        new
        {
            url = service.GetClientAccessUri().AbsoluteUri
        };
});
app.Run();

sealed class NotificationService([FromKeyedServices("notificationHub")] WebPubSubServiceClient service) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(5000, stoppingToken);

            // periodically broadcast messages to the client
            await service.SendToAllAsync($"{DateTime.Now}: Hello from background service.");
        }
    }
}
