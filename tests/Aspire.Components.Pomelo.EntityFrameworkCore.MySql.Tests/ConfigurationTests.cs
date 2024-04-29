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
        => Assert.False(new PomeloEntityFrameworkCoreMySqlSettings().DisableHealthChecks);

    [Fact]
    public void TracingIsEnabledByDefault()
        => Assert.False(new PomeloEntityFrameworkCoreMySqlSettings().DisableTracing);

    [Fact]
    public void MetricsAreEnabledByDefault()
        => Assert.False(new PomeloEntityFrameworkCoreMySqlSettings().DisableMetrics);

    [Fact]
    public void RetriesAreEnabledByDefault()
        => Assert.False(new PomeloEntityFrameworkCoreMySqlSettings().DisableRetry);
}
