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
        => Assert.True(new NpgsqlEntityFrameworkCorePostgreSQLSettings().HealthChecksEnabled);

    [Fact]
    public void TracingIsEnabledByDefault()
        => Assert.True(new NpgsqlEntityFrameworkCorePostgreSQLSettings().TracingEnabled);

    [Fact]
    public void MetricsAreEnabledByDefault()
        => Assert.True(new NpgsqlEntityFrameworkCorePostgreSQLSettings().MetricsEnabled);

    [Fact]
    public void RetriesAreEnabledByDefault()
        => Assert.True(new NpgsqlEntityFrameworkCorePostgreSQLSettings().RetryEnabled);
}
