var builder = DistributedApplication.CreateBuilder(args);

builder.AddDapr();

var stateStore = builder.AddDaprStateStore("statestore");
var pubSub = builder.AddDaprPubSub("pubsub");

builder.AddProject<Projects.DaprServiceA>("servicea")
       .WithDaprSidecar("service-a")
       .WithReference(stateStore)
       .WithReference(pubSub);

builder.AddProject<Projects.DaprServiceB>("serviceb")
       .WithDaprSidecar("service-b")
       .WithReference(pubSub);

using var app = builder.Build();

await app.RunAsync();
