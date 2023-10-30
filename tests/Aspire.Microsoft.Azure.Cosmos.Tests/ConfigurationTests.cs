// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Microsoft.Azure.Cosmos.Tests;

public class ConfigurationTests
{
    [Fact]
    public void ConnectionStringIsNullByDefault()
        => Assert.Null(new AzureDataCosmosSettings().ConnectionString);

    [Fact]
    public void TracingIsEnabledByDefault()
        => Assert.True(new AzureDataCosmosSettings().Tracing);

    [Fact]
    public void MetricsAreEnabledByDefault()
        => Assert.True(new AzureDataCosmosSettings().Metrics);
}
