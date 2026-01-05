// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Otlp.Model;

/// <summary>
/// Defines how a metric aggregator reports aggregated values.
/// </summary>
public enum OtlpAggregationTemporality
{
    /// <summary>
    /// The default value, indicating the aggregation temporality is not specified.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Reports changes since the last report time.
    /// </summary>
    Delta = 1,

    /// <summary>
    /// Reports changes since a fixed start time.
    /// </summary>
    Cumulative = 2
}
