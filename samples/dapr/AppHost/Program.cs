using Aspire.Hosting.Dapr;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDapr();

builder.AddProject<DaprAppHost.Projects.DaprServiceA>()
       .WithDaprSidecar("service-a");

builder.AddProject<DaprAppHost.Projects.DaprServiceB>()
       .WithDaprSidecar("service-b");

using var app = builder.Build();

await app.RunAsync();
