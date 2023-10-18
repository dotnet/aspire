var builder = DistributedApplication.CreateBuilder(args);

builder.AddDapr();

builder.AddProject<Projects.DaprServiceA>("servicea")
       .WithDaprSidecar("service-a");

builder.AddProject<Projects.DaprServiceB>("serviceb")
       .WithDaprSidecar("service-b");

using var app = builder.Build();

await app.RunAsync();
