
var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

var wps = builder.AddAzureWebPubSub("wps1");

builder.AddProject<Projects.WebPubSubWeb>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(wps);

builder.Build().Run();
