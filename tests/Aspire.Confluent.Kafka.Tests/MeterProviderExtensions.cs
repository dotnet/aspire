// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OpenTelemetry.Metrics;

namespace Aspire.Confluent.Kafka.Tests;
internal static class MeterProviderExtensions
{
    public static void EnsureMetricsAreFlushed(this MeterProvider meterProvider)
    {
        while (!meterProvider.ForceFlush())
        {
        }
    }
}
