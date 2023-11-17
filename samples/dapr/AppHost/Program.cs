using Aspire.Hosting.Dapr;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDapr();

var stateStore = builder.AddDaprStateStore("statestore");
var pubSub = builder.AddDaprPubSub("pubsub");

builder.AddProject<Projects.DaprServiceA>("servicea")
       .WithDaprSidecar(new DaprSidecarOptions { AppId = "service-a", LogLevel = "debug" })
       .WithReference(stateStore)
       .WithReference(pubSub);

builder.AddProject<Projects.DaprServiceB>("serviceb")
       .WithDaprSidecar(new DaprSidecarOptions { AppId = "service-b", LogLevel = "debug" })
       .WithReference(pubSub);

using var app = builder.Build();

await app.RunAsync();
