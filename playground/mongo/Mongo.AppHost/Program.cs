// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
var builder = DistributedApplication.CreateBuilder(args);

var db = builder.AddMongoDB("mongo")
    .WithMongoExpress(c => c.WithHostPort(3022))
    .PublishAsContainer();

builder.AddProject<Projects.Mongo_ApiService>("api")
       .WithExternalHttpEndpoints()
       .WithReference(db);

#if BUILD_FOR_TEST
builder.Services.AddLifecycleHook<EndPointWriterHook>();
#endif

#if !TESTS_RUNNING_OUTSIDE_OF_REPO

// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// to test end developer dashboard launch experience. Refer to Directory.Build.props
// for the path to the dashboard binary (defaults to the Aspire.Dashboard bin output
// in the artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);

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
