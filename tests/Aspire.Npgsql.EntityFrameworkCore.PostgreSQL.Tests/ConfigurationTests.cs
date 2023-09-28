// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Xunit;

namespace Aspire.Npgsql.EntityFrameworkCore.PostgreSQL.Tests;

public class ConfigurationTests
{
    [Fact]
    public void ConnectionStringIsNullByDefault()
        => Assert.Null(new NpgsqlEntityFrameworkCorePostgreSQLSettings().ConnectionString);

    [Fact]
    public void DbContextPoolingIsEnabledByDefault()
        => Assert.True(new NpgsqlEntityFrameworkCorePostgreSQLSettings().DbContextPooling);

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
    public void MaxRetryCountIsSameAsInTheDefaultNpgsqlPolicy()
    {
        DbContextOptionsBuilder<TestDbContext> dbContextOptionsBuilder = new();
        dbContextOptionsBuilder.UseNpgsql("fakeConnectionString");
        TestDbContext dbContext = new(dbContextOptionsBuilder.Options);

        Assert.Equal(new WorkaroundToReadProtectedField(dbContext).RetryCount, new NpgsqlEntityFrameworkCorePostgreSQLSettings().MaxRetryCount);
    }

    public class WorkaroundToReadProtectedField : NpgsqlRetryingExecutionStrategy
    {
        public WorkaroundToReadProtectedField(DbContext context) : base(context)
        {
        }

        public int RetryCount => base.MaxRetryCount;
    }
}
