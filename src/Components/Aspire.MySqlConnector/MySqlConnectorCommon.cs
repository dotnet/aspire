// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OpenTelemetry.Metrics;

internal static class MySqlConnectorCommon
{
    public static void AddMySqlMetrics(MeterProviderBuilder meterProviderBuilder)
    {
        meterProviderBuilder
            .AddMeter("MySqlConnector")
            .AddView("db.client.connections.create_time", new HistogramConfiguration());
    }
}
