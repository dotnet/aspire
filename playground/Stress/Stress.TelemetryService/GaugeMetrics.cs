// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Stress.TelemetryService;

public class GaugeMetrics(ILogger<GaugeMetrics> logger, IMeterFactory meterFactory) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting GaugeMetrics");

        var process = Process.GetCurrentProcess();

        var meter = meterFactory.Create("GaugeMetrics");
        meter.CreateObservableGauge(
            "ProcessWorkingSetGauge",
            () =>
            {
                var measurements = new List<Measurement<long>>();
                var processes = Process.GetProcesses().ToList();

                foreach (var process in processes)
                {
                    var workingSet = process.WorkingSet64;
                    measurements.Add(new Measurement<long>(workingSet, new KeyValuePair<string, object?>("process.id", process.Id)));
                }

                return measurements;
            });

        return Task.CompletedTask;
    }
}
