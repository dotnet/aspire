// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Stress.ApiService;

public class TestMetrics : IDisposable
{
    public const string MeterName = "TestMeter";

    private readonly Meter _meter;
    private readonly Counter<int> _counter;

    public TestMetrics()
    {
        _meter = new Meter(MeterName, "1.0.0",
        [
            new KeyValuePair<string, object?>("meter-tag", Guid.NewGuid().ToString())
        ]);

        _counter = _meter.CreateCounter<int>("test-counter", unit: null, description: "This is a description", tags:
        [
            new KeyValuePair<string, object?>("instrument-tag", Guid.NewGuid().ToString())
        ]);

        var uploadSpeed = new List<double>();

        Task.Run(async () =>
        {
            while (true)
            {
                lock (uploadSpeed)
                {
                    uploadSpeed.Add(Random.Shared.Next(5, 10));
                }
                await Task.Delay(1000);
            }
        });

        _meter.CreateObservableGauge<double>("observable-gauge", () =>
        {
            lock (uploadSpeed)
            {
                var sum = 0d;
                for (var i = 0; i < uploadSpeed.Count; i++)
                {
                    sum += uploadSpeed[i];
                }
                return new Measurement<double>(sum / uploadSpeed.Count);
            }
        }, unit: "By/s");
    }

    public void IncrementCounter(int value, in TagList tags)
    {
        _counter.Add(value, in tags);
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
