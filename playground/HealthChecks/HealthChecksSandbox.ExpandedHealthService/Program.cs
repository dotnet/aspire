// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks()
    .AddCheck("database", () => HealthCheckResult.Healthy("Database connection is healthy"))
    .AddCheck("cache", () => HealthCheckResult.Degraded("Cache is slow but functional"))
    .AddCheck("message_queue", () => HealthCheckResult.Unhealthy("Message queue is unavailable"));

var app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = AspireHealthCheckResponseWriter.WriteResponse
});

app.Run();
