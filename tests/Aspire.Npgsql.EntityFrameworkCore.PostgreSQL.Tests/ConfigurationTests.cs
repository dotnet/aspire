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
        => Assert.True(new NpgsqlEntityFrameworkCorePostgreSQLSettings().HealthChecks);

    [Fact]
    public void TracingIsEnabledByDefault()
        => Assert.True(new NpgsqlEntityFrameworkCorePostgreSQLSettings().Tracing);

    [Fact]
    public void MetricsAreEnabledByDefault()
        => Assert.True(new NpgsqlEntityFrameworkCorePostgreSQLSettings().Metrics);

    [Fact]
    public void RetriesAreEnabledByDefault()
        => Assert.True(new NpgsqlEntityFrameworkCorePostgreSQLSettings().Retry);
}
