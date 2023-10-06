using Aspire.Hosting.Dapr;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDapr();

builder.AddProject<DaprApp.Projects.DaprServiceA>()
       .WithDaprSidecar("service-a");

builder.AddProject<DaprApp.Projects.DaprServiceB>()
       .WithDaprSidecar("service-b");

using var app = builder.Build();

await app.RunAsync();
