// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.Configuration["ConnectionStrings:cs"] = "testconnection";

builder.AddConnectionString("cs");
builder.AddRedis("redis1");
var webApp = builder.AddProject<Projects.TestingAppHost1_MyWebApp>("mywebapp1")
    .WithEnvironment("APP_HOST_ARG", builder.Configuration["APP_HOST_ARG"])
    .WithEnvironment("LAUNCH_PROFILE_VAR_FROM_APP_HOST", builder.Configuration["LAUNCH_PROFILE_VAR_FROM_APP_HOST"]);

if (builder.Configuration.GetValue("USE_HTTPS", false))
{
    webApp.WithExternalHttpEndpoints();
}

builder.AddProject<Projects.TestingAppHost1_MyWorker>("myworker1")
    .WithEndpoint(name: "myendpoint1");
builder.AddPostgres("postgres1");

if (args.Contains("--crash-before-build"))
{
    throw new InvalidOperationException("Crashing: before-build.");
}

var app = builder.Build();

if (args.Contains("--crash-after-build"))
{
    throw new InvalidOperationException("Crashing: after-build.");
}

await app.StartAsync();

if (args.Contains("--crash-after-start"))
{
    throw new InvalidOperationException("Crashing: after-start.");
}

await app.WaitForShutdownAsync();

if (args.Contains("--crash-after-shutdown"))
{
    throw new InvalidOperationException("Crashing after-shutdown.");
}
