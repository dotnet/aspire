var builder = DistributedApplication.CreateBuilder(args);

builder.AddDapr();

builder.AddProject<Projects.MetricsApp>("app");

using var app = builder.Build();

await app.RunAsync();
