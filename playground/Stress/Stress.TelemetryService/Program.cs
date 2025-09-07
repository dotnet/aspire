// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Stress.TelemetryService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<TelemetryStresser>();
builder.Services.AddHostedService<GaugeMetrics>();
builder.Services.AddHostedService<CounterMetrics>();

builder.AddServiceDefaults();
builder.Logging.SetMinimumLevel(LogLevel.Trace);

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("GaugeMetrics");
        metrics.AddMeter("CounterMetrics");
    });

var app = builder.Build();

app.Run();
