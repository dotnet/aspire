using Azure.Messaging.WebPubSub;

var builder = WebApplication.CreateBuilder(args);

builder.AddKeyedAzureWebPubSubServiceClient(Constants.ChatHubName);
builder.AddKeyedAzureWebPubSubServiceClient(Constants.NotificationHubName);

// add a background service to periodically broadcast messages to the client
builder.Services.AddHostedService<NotificationService>();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpForwarder();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

var chatEndpoint = app.Services.GetRequiredKeyedService<WebPubSubServiceClient>(Constants.ChatHubName).Endpoint;
if (chatEndpoint.IsLoopback)
{
    // A browser outside a Codespace cannot reach the emulator's loopback address.
    // Proxy client connections through the web endpoint so they also share its HTTPS and port authentication.
    app.MapForwarder("/client/hubs/{**path}", chatEndpoint.AbsoluteUri);
}

// return the Client Access URL with negotiate endpoint
app.MapGet("/negotiate/chat", ([FromKeyedServices(Constants.ChatHubName)] WebPubSubServiceClient service) =>
{
    var clientAccessUri = service.GetClientAccessUri(roles: ["webpubsub.sendToGroup.group1", "webpubsub.joinLeaveGroup.group1"]);
    return
        new
        {
            url = service.Endpoint.IsLoopback ? clientAccessUri.PathAndQuery : clientAccessUri.AbsoluteUri
        };
});

app.MapGet("/negotiate/notification", ([FromKeyedServices(Constants.NotificationHubName)] WebPubSubServiceClient service) =>
{
    var clientAccessUri = service.GetClientAccessUri();
    return
        new
        {
            url = service.Endpoint.IsLoopback ? clientAccessUri.PathAndQuery : clientAccessUri.AbsoluteUri
        };
});

// handle events for chat
app.Map($"/eventhandler/{Constants.ChatHubName}", async ([FromKeyedServices(Constants.ChatHubName)] WebPubSubServiceClient service, HttpContext context, ILogger<Program> logger) =>
{
    context.Response.Headers["WebHook-Allowed-Origin"] = "*";
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 200;
        return;
    }

    if (context.Request.Method != "POST" || !context.Request.Headers.TryGetValue("ce-type", out var eventType))
    {
        context.Response.StatusCode = 400;
        return;
    }
    context.Response.StatusCode = 200;
    var connectionId = context.Request.Headers["ce-connectionId"];
    if (eventType == "azure.webpubsub.sys.connected")
    {
        logger.LogInformation("[SYSTEM] {ConnectionId} joined.", connectionId.ToString());
        await service.SendToAllAsync($"[SYSTEM] {connectionId} joined.");
    }
});

app.Run();

sealed class NotificationService([FromKeyedServices(Constants.NotificationHubName)] WebPubSubServiceClient service) : BackgroundService
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

static class Constants
{
    public const string ChatHubName = "ChatForAspire";
    public const string NotificationHubName = "NotificationForAspire";
}
