// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Microsoft.Extensions.Configuration.AzureAppConfiguration.Tests;

public class ConfigurationTests
{
    [Fact]
    public void EndpointUriIsNullByDefault()
        => Assert.Null(new AzureAppConfigurationSettings().Endpoint);

    [Fact]
    public void TracingIsEnabledByDefault()
        => Assert.False(new AzureAppConfigurationSettings().DisableTracing);
}
