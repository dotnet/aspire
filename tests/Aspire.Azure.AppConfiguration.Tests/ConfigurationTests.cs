// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Azure.AppConfiguration.Tests;

public class ConfigurationTests
{
    [Fact]
    public void EndpointUriIsNullByDefault()
        => Assert.Null(new AzureAppConfigurationSettings().Endpoint);

    // WIP: https://github.com/Azure/AppConfiguration-DotnetProvider/pull/645
    // Tracing will be supported in the next 8.2.0 release
}
