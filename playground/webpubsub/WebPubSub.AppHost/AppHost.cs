var builder = DistributedApplication.CreateBuilder(args);

var wps = builder.AddAzureWebPubSub("wps1").RunAsEmulator();
var chat = wps.AddHub("ChatForAspire");
var notification = wps.AddHub("NotificationForAspire");
var web = builder.AddProject<Projects.WebPubSubWeb>("webfrontend", launchProfileName: builder.ExecutionContext.IsPublishMode ? "https" : "http")
    .WithExternalHttpEndpoints()
    .WithReference(chat)
    .WithReference(notification)
    .WaitFor(wps);

var callbackEndpoint = web.GetEndpoint(builder.ExecutionContext.IsPublishMode ? "https" : "http");
chat.AddEventHandler($"{callbackEndpoint}/eventhandler/{chat.Resource.HubName}", systemEvents: ["connected"]);

builder.Build().Run();
