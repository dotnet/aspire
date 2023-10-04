using Aspire.Hosting.Dapr;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDapr();

builder.AddProject<DaprDevHost.Projects.DaprServiceA>()
       .WithDaprSidecar("service-a");

builder.AddProject<DaprDevHost.Projects.DaprServiceB>()
       .WithDaprSidecar("service-b");

using var app = builder.Build();

await app.RunAsync();
