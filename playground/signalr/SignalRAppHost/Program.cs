var builder = DistributedApplication.CreateBuilder(args);

var signalr = builder.AddAzureSignalR("signalr1");

builder.AddProject<Projects.SignalRWeb>("webfrontend")
    .WithReference(signalr);

builder.Build().Run();
