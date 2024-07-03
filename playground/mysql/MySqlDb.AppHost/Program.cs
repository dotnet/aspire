// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var catalogDbName = "catalog"; // MySql database & table names are case-sensitive on non-Windows.
var catalogDb = builder.AddMySql("mysql")
    .WithEnvironment("MYSQL_DATABASE", catalogDbName)
    .WithBindMount("../MySql.ApiService/data", "/docker-entrypoint-initdb.d")
    .WithPhpMyAdmin()
    .AddDatabase(catalogDbName);

builder.AddProject<Projects.MySql_ApiService>("apiservice")
    .WithExternalHttpEndpoints()
    .WithReference(catalogDb);

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
