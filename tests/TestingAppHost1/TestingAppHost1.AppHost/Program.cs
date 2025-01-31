// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.Configuration["ConnectionStrings:cs"] = "testconnection";

builder.AddConnectionString("cs");
builder.AddRedis("redis1");
builder.AddProject<Projects.TestingAppHost1_MyWebApp>("mywebapp1")
    .WithEndpoint("http", ea => ea.IsProxied = false)
    .WithEndpoint("https", ea => ea.IsProxied = false)
    .WithExternalHttpEndpoints();
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
