// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aspire.Pomelo.EntityFrameworkCore.MySql.Tests;

public class ConfigurationTests
{
    [Fact]
    public void ConnectionStringIsNullByDefault()
        => Assert.Null(new PomeloEntityFrameworkCoreMySqlSettings().ConnectionString);

    [Fact]
    public void DbContextPoolingIsEnabledByDefault()
        => Assert.True(new PomeloEntityFrameworkCoreMySqlSettings().DbContextPooling);

    [Fact]
    public void HealthCheckIsEnabledByDefault()
        => Assert.True(new PomeloEntityFrameworkCoreMySqlSettings().HealthChecks);

    [Fact]
    public void TracingIsEnabledByDefault()
        => Assert.True(new PomeloEntityFrameworkCoreMySqlSettings().Tracing);

    [Fact]
    public void MetricsAreEnabledByDefault()
        => Assert.True(new PomeloEntityFrameworkCoreMySqlSettings().Metrics);

    [Fact]
    public void MaxRetryCountIsSameAsInTheDefaultPomeloPolicy()
    {
        DbContextOptionsBuilder<TestDbContext> dbContextOptionsBuilder = new();
        dbContextOptionsBuilder.UseMySql("Server=fake", new MySqlServerVersion(new Version(8, 2, 0)));
        TestDbContext dbContext = new(dbContextOptionsBuilder.Options);

        Assert.Equal(new WorkaroundToReadProtectedField(dbContext).RetryCount, new PomeloEntityFrameworkCoreMySqlSettings().MaxRetryCount);
    }

    public class WorkaroundToReadProtectedField : MySqlRetryingExecutionStrategy
    {
        public WorkaroundToReadProtectedField(DbContext context) : base(context)
        {
        }

        public int RetryCount => base.MaxRetryCount;
    }
}
