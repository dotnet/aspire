// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.NATS.Net.Tests;

public class ConfigurationTests
{
    [Fact]
    public void ConnectionStringIsNullByDefault()
        => Assert.Null(new NatsClientSettings().ConnectionString);

    [Fact]
    public void HealthCheckIsEnabledByDefault()
        => Assert.True(new NatsClientSettings().HealthChecks);

    [Fact]
    public void TracingIsEnabledByDefault()
        => Assert.True(new NatsClientSettings().Tracing);
}
