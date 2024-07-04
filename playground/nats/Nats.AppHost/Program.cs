var builder = DistributedApplication.CreateBuilder(args);

var nats = builder.AddNats("nats")
    .WithJetStream();

builder.AddProject<Projects.Nats_ApiService>("api")
    .WithExternalHttpEndpoints()
    .WithReference(nats);

builder.AddProject<Projects.Nats_Backend>("backend")
    .WithReference(nats);

#if BUILD_FOR_TEST
builder.Services.AddLifecycleHook<EndPointWriterHook>();
#endif

using var app = builder.Build();

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

await app.RunAsync();
