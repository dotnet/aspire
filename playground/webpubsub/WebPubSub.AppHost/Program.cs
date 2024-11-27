
var builder = DistributedApplication.CreateBuilder(args);

var wps = builder.AddAzureWebPubSub("wps1");
var web = builder.AddProject<Projects.WebPubSubWeb>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(wps);

// Now only works in production since localhost is not accessible from Azure
if (builder.ExecutionContext.IsPublishMode)
{
    wps.AddHub("ChatForAspire").AddEventHandler($"{web.GetEndpoint("https")}/eventhandler/", systemEvents: ["connected"]);
}

builder.Build().Run();
