// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var queue = storage.AddQueues("queue");
var blob = storage.AddBlobs("blob");

var signalr = builder
    .AddAzureSignalR("signalr1")
    .RunAsEmulator();

builder.AddAzureFunctionsProject<Projects.SignalRServerless_Functions>("funcapp")
    .WithHostStorage(storage)
    .WithExternalHttpEndpoints()
    .WithEnvironment("SignalR_ConnectionString", signalr) // Injected connection string as env variable
    .WithReference(signalr)
    .WithReference(blob)
    .WithReference(queue);

builder.Build().Run();
