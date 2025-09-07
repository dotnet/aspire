// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Azure.Npgsql.EntityFrameworkCore.PostgreSQL.Tests;

public class ConfigurationTests
{
    [Fact]
    public void ConnectionStringIsNullByDefault()
        => Assert.Null(new AzureNpgsqlEntityFrameworkCorePostgreSQLSettings().ConnectionString);

    [Fact]
    public void HealthCheckIsEnabledByDefault()
        => Assert.False(new AzureNpgsqlEntityFrameworkCorePostgreSQLSettings().DisableHealthChecks);

    [Fact]
    public void TracingIsEnabledByDefault()
        => Assert.False(new AzureNpgsqlEntityFrameworkCorePostgreSQLSettings().DisableTracing);

    [Fact]
    public void MetricsAreEnabledByDefault()
        => Assert.False(new AzureNpgsqlEntityFrameworkCorePostgreSQLSettings().DisableMetrics);

    [Fact]
    public void RetriesAreEnabledByDefault()
        => Assert.False(new AzureNpgsqlEntityFrameworkCorePostgreSQLSettings().DisableRetry);
}
