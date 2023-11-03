// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OpenTelemetry.Metrics;

internal static class NpgsqlCommon
{
    public static void AddNpgsqlMetrics(MeterProviderBuilder meterProviderBuilder)
    {
        double[] secondsBuckets = [0, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10];

        // https://github.com/npgsql/npgsql/blob/4c9921de2dfb48fb5a488787fc7422add3553f50/src/Npgsql/MetricsReporter.cs#L48
        meterProviderBuilder
            .AddMeter("Npgsql")
            // Npgsql's histograms are in seconds, not milliseconds.
            .AddView("db.client.commands.duration",
                new ExplicitBucketHistogramConfiguration
                {
                    Boundaries = secondsBuckets
                })
            .AddView("db.client.connections.create_time",
                new ExplicitBucketHistogramConfiguration
                {
                    Boundaries = secondsBuckets
                });
    }
}
