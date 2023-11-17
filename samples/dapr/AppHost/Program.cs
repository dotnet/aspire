var builder = DistributedApplication.CreateBuilder(args);

builder.AddDapr();

var stateStore = builder.AddDaprStateStore("statestore");

builder.AddProject<Projects.DaprServiceA>("servicea")
       .WithDaprSidecar("service-a")
       .WithReference(stateStore);

builder.AddProject<Projects.DaprServiceB>("serviceb")
       .WithDaprSidecar("service-b");

using var app = builder.Build();

await app.RunAsync();
