// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Stress.ApiService;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(BigTraceCreator.ActivitySourceName));

var app = builder.Build();

app.Lifetime.ApplicationStarted.Register(ConsoleStresser.Stress);

app.MapGet("/", () => "Hello world");

app.MapGet("/big-trace", async () =>
{
    var bigTraceCreator = new BigTraceCreator();

    await bigTraceCreator.CreateBigTraceAsync();

    return "Big trace created";
});

app.Run();
