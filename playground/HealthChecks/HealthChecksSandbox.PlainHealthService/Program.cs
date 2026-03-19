// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHealthChecks()
    .AddCheck("database", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Database connection is healthy"))
    .AddCheck("cache", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded("Cache is slow but functional"))
    .AddCheck("message_queue", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Message queue is unavailable"));

var app = builder.Build();
app.MapDefaultEndpoints();
app.Run();
