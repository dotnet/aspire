// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var qdrant = builder.AddQdrant("qdrant")
    .WithDataVolume("qdrant-data");

builder.AddProject<Projects.Qdrant_ApiService>("apiservice")
    .WithExternalHttpEndpoints()
    .WithReference(qdrant);

#if BUILD_FOR_TEST
builder.Services.AddLifecycleHook<EndPointWriterHook>();
#endif

var app = builder.Build();

#if BUILD_FOR_TEST
// Run a task to read from the console and stop the app if an external process sends "Stop".
// This allows for easier control than sending CTRL+C to the console in a cross-platform way.
_ = Task.Run(async () =>
{
    var s = Console.ReadLine();
    if (s == "Stop")
    {
        await app.StopAsync();
    }
});
#endif

app.Run();
