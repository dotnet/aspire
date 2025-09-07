// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;

namespace Stress.TelemetryService;

public class CounterMetrics(ILogger<CounterMetrics> logger, IMeterFactory meterFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting CounterMetrics");

        var meter = meterFactory.Create("CounterMetrics");
        var counter = meter.CreateCounter<int>(
            name: "run.done.new.count",
            description: "Count of new done run",
            unit: "count");

        for (var i = 0; i < 1000000; i++)
        {
            counter.Add(1);
            await Task.Delay(20, cancellationToken);
        }
    }
}
