using Aspire.Hosting.Dapr;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDapr();

//string resourcesRelativePath = @"../Resources";
//string stateStoreRelativePath = Path.Combine(resourcesRelativePath, "statestore.yaml");

var stateStore = builder.AddDaprComponent("statestore", "statestore", new DaprComponentOptions { /* LocalPath = stateStoreRelativePath */ });

builder.AddProject<Projects.DaprServiceA>("servicea")
       .WithDaprSidecar("service-a")
       .WithReference(stateStore);

builder.AddProject<Projects.DaprServiceB>("serviceb")
       .WithDaprSidecar("service-b");

using var app = builder.Build();

await app.RunAsync();
