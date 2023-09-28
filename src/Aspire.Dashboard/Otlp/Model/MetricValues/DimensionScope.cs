// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Metrics.V1;

namespace Aspire.Dashboard.Otlp.Model.MetricValues;

public class DimensionScope
{
    public string Name { get; init; }
    public int Key { get; init; }

    public readonly KeyValuePair<string, string>[] _dimensions;
    public readonly List<MetricValueBase> _values = new();

    // Used to aid in merging values that are the same in a concurrent environment
    private MetricValueBase? _lastValue;

    public IEnumerable<MetricValueBase> Values => _values;

    public bool IsHistogram => _values.Count > 0 && _values[0] is HistogramValue;

    public DimensionScope(int key, RepeatedField<KeyValue> keyvalues)
    {
        _dimensions = keyvalues.ToKeyValuePairs();
        var name = _dimensions.ConcatProperties();
        Name = name != null && name.Length > 0 ? name : "no-dimensions";
        Key = key;
    }

    /// <summary>
    /// Compares and updates the timespan for metrics if they are unchanged.
    /// </summary>
    /// <param name="d">Metric value to merge</param>
    public void AddPointValue(NumberDataPoint d)
    {
        var start = OtlpHelpers.UnixNanoSecondsToDateTime(d.StartTimeUnixNano);
        var end = OtlpHelpers.UnixNanoSecondsToDateTime(d.TimeUnixNano);

        if (d.ValueCase == NumberDataPoint.ValueOneofCase.AsInt)
        {
            var value = d.AsInt;
            lock (this)
            {
                var lastLongValue = _lastValue as MetricValue<long>;
                if (lastLongValue is not null && lastLongValue.Value == value)
                {
                    lastLongValue.End = end;
                    Interlocked.Increment(ref lastLongValue.Count);
                }
                else
                {
                    if (lastLongValue is not null)
                    {
                        start = lastLongValue.End;
                    }
                    _lastValue = new MetricValue<long>(d.AsInt, start, end);
                    _values.Append(_lastValue);
                    //_values.Add(_lastValue);
                }
            }
        }
        else if (d.ValueCase == NumberDataPoint.ValueOneofCase.AsDouble)
        {
            lock (this)
            {
                var lastDoubleValue = _lastValue as MetricValue<double>;
                if (lastDoubleValue is not null && lastDoubleValue.Value == d.AsDouble)
                {
                    lastDoubleValue.End = end;
                    Interlocked.Increment(ref lastDoubleValue.Count);
                }
                else
                {
                    if (lastDoubleValue is not null)
                    {
                        start = lastDoubleValue.End;
                    }
                    _lastValue = new MetricValue<double>(d.AsDouble, start, end);
                    //_values.(_lastValue);
                    _values.Append(_lastValue);
                }
            }
        }
    }

    public void AddHistogramValue(HistogramDataPoint h)
    {
        var start = OtlpHelpers.UnixNanoSecondsToDateTime(h.StartTimeUnixNano);
        var end = OtlpHelpers.UnixNanoSecondsToDateTime(h.TimeUnixNano);
        lock (this)
        {
            var lastHistogramValue = _lastValue as HistogramValue;
            if (lastHistogramValue is not null && lastHistogramValue.Count == h.Count)
            {
                lastHistogramValue.End = end;
            }
            else
            {
                if (lastHistogramValue is not null)
                {
                    start = lastHistogramValue.End;
                }
                _lastValue = new HistogramValue(h.BucketCounts, h.Sum, h.Count, start, end, h.ExplicitBounds.ToArray());
                //_values.Add(_lastValue);
                _values.Append(_lastValue);
            }
        }
    }
}
