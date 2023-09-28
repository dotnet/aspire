// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Dashboard.Otlp.Model.MetricValues;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Metrics.V1;

namespace Aspire.Dashboard.Otlp.Model;

[DebuggerDisplay("Name = {Name}, Unit = {Unit}, Type = {Type}")]
public class OtlpInstrument
{
    public string Name { get; init; }
    public string Description { get; init; }
    public string Unit { get; init; }
    public Metric.DataOneofCase Type { get; init; }
    public OtlpMeter Parent { get; init; }

    public Dictionary<int, DimensionScope> Dimensions { get; } = new();

    public OtlpInstrument(Metric mData, OtlpMeter parent)
    {
        Name = mData.Name;
        Description = mData.Description;
        Unit = mData.Unit;
        Type = mData.DataCase;
        Parent = parent;
    }

    public void AddInstrumentValuesFromGrpc(Metric mData)
    {
        switch (mData.DataCase)
        {
            case Metric.DataOneofCase.Gauge:
                foreach (var d in mData.Gauge.DataPoints)
                {
                    FindScope(d.Attributes).AddPointValue(d);
                }
                break;
            case Metric.DataOneofCase.Sum:
                foreach (var d in mData.Sum.DataPoints)
                {
                    FindScope(d.Attributes).AddPointValue(d);
                }
                break;
            case Metric.DataOneofCase.Histogram:
                foreach (var d in mData.Histogram.DataPoints)
                {
                    FindScope(d.Attributes).AddHistogramValue(d);
                }
                break;
        }
    }

    private DimensionScope FindScope(RepeatedField<KeyValue> attributes)
    {
        var key = CalculateDimensionHashcode(attributes);
        if (!Dimensions.TryGetValue(key, out var dimension))
        {
            Dimensions.Add(key, dimension = new DimensionScope(key, attributes));
        }
        return dimension;
    }

    /// <summary>
    /// Creates a hashcode for a dimension based on the dimension key/value pairs.
    /// </summary>
    /// <param name="keyvalues">The keyvalue pairs of the dimension</param>
    /// <returns>A hashcode</returns>
    private static int CalculateDimensionHashcode(RepeatedField<KeyValue> keyvalues)
    {
        if (keyvalues is null || keyvalues.Count == 0)
        {
            return 0;
        }
        var dict = keyvalues.ToKeyValuePairs();

        var hash = new HashCode();
        foreach (var kv in dict.OrderBy(x => x.Key))
        {
            hash.Add(kv.Key);
            hash.Add(kv.Value);
        }
        return hash.ToHashCode();
    }
}
