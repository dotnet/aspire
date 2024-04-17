// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Npgsql.EntityFrameworkCore.PostgreSQL.Tests;

public class ConfigurationTests
{
    [Fact]
    public void ConnectionStringIsNullByDefault()
        => Assert.Null(new NpgsqlEntityFrameworkCorePostgreSQLSettings().ConnectionString);

    [Fact]
    public void HealthCheckIsEnabledByDefault()
        => Assert.False(new NpgsqlEntityFrameworkCorePostgreSQLSettings().DisableHealthChecks);

    [Fact]
    public void TracingIsEnabledByDefault()
        => Assert.False(new NpgsqlEntityFrameworkCorePostgreSQLSettings().DisableTracing);

    [Fact]
    public void MetricsAreEnabledByDefault()
        => Assert.False(new NpgsqlEntityFrameworkCorePostgreSQLSettings().DisableMetrics);

    [Fact]
    public void RetriesAreEnabledByDefault()
        => Assert.False(new NpgsqlEntityFrameworkCorePostgreSQLSettings().DisableRetry);
}
