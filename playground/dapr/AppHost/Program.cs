using Aspire.Hosting.Dapr;

var builder = DistributedApplication.CreateBuilder(args);

var stateStore = builder.AddDaprStateStore("statestore");
var pubSub = builder.AddDaprPubSub("pubsub");

builder.AddProject<Projects.DaprServiceA>("servicea", "https")
       .WithDaprSidecar(new DaprSidecarOptions{
              AppProtocol = "https"
       })
       .WithReference(stateStore)
       .WithReference(pubSub);

builder.AddProject<Projects.DaprServiceB>("serviceb", "https")
       .WithDaprSidecar(new DaprSidecarOptions{
              AppProtocol = "https"
       })
       .WithReference(pubSub);

// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// to test end developer dashboard launch experience. Refer to Directory.Build.props
// for the path to the dashboard binary (defaults to the Aspire.Dashboard bin output
// in the artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);

using var app = builder.Build();

await app.RunAsync();
