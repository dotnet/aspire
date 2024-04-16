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
        => Assert.True(new PomeloEntityFrameworkCoreMySqlSettings().HealthChecksEnabled);

    [Fact]
    public void TracingIsEnabledByDefault()
        => Assert.True(new PomeloEntityFrameworkCoreMySqlSettings().TracingEnabled);

    [Fact]
    public void MetricsAreEnabledByDefault()
        => Assert.True(new PomeloEntityFrameworkCoreMySqlSettings().MetricsEnabled);

    [Fact]
    public void RetriesAreEnabledByDefault()
        => Assert.True(new PomeloEntityFrameworkCoreMySqlSettings().RetryEnabled);
}
