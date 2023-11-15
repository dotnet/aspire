// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.MySqlConnector.Tests;

public class ConfigurationTests
{
    [Fact]
    public void ConnectionStringIsNullByDefault()
    => Assert.Null(new MySqlConnectorSettings().ConnectionString);

    [Fact]
    public void HealthCheckIsEnabledByDefault()
        => Assert.True(new MySqlConnectorSettings().HealthChecks);

    [Fact]
    public void TracingIsEnabledByDefault()
        => Assert.True(new MySqlConnectorSettings().Tracing);

    [Fact]
    public void MetricsAreEnabledByDefault()
        => Assert.True(new MySqlConnectorSettings().Metrics);
}
