// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.ClickHouse.Driver.Tests;

public class ConfigurationTests
{
    [Fact]
    public void ConnectionStringIsNullByDefault()
        => Assert.Null(new ClickHouseClientSettings().ConnectionString);

    [Fact]
    public void HealthCheckIsEnabledByDefault()
        => Assert.False(new ClickHouseClientSettings().DisableHealthChecks);

    [Fact]
    public void TracingIsEnabledByDefault()
        => Assert.False(new ClickHouseClientSettings().DisableTracing);

    [Fact]
    public void MetricsAreDisabledByDefault()
        => Assert.True(new ClickHouseClientSettings().DisableMetrics);

    [Fact]
    public void HealthCheckTimeoutIsNullByDefault()
        => Assert.Null(new ClickHouseClientSettings().HealthCheckTimeout);
}
