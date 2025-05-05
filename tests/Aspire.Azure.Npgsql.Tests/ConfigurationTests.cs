// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Azure.Npgsql.Tests;

public class ConfigurationTests
{
    [Fact]
    public void ConnectionStringIsNullByDefault()
    => Assert.Null(new AzureNpgsqlSettings().ConnectionString);

    [Fact]
    public void HealthCheckIsEnabledByDefault()
        => Assert.False(new AzureNpgsqlSettings().DisableHealthChecks);

    [Fact]
    public void TracingIsEnabledByDefault()
        => Assert.False(new AzureNpgsqlSettings().DisableTracing);

    [Fact]
    public void MetricsAreEnabledByDefault()
        => Assert.False(new AzureNpgsqlSettings().DisableMetrics);
}
