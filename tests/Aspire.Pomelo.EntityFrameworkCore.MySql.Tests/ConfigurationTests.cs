// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Pomelo.EntityFrameworkCore.MySql.Tests;

public class ConfigurationTests
{
    [Fact]
    public void ConnectionStringIsNullByDefault()
        => Assert.Null(new PomeloEntityFrameworkCoreMySqlSettings().ConnectionString);

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
    public void RetriesAreEnabledByDefault()
        => Assert.True(new PomeloEntityFrameworkCoreMySqlSettings().Retry);
}
