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
        _meter = new Meter(MeterName, "1.0.0", new[]
        {
            new KeyValuePair<string, object?>("meter-tag", Guid.NewGuid().ToString())
        });

        _counter = _meter.CreateCounter<int>("test-counter", unit: null, description: null, tags: new[]
        {
            new KeyValuePair<string, object?>("instrument-tag", Guid.NewGuid().ToString())
        });
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
