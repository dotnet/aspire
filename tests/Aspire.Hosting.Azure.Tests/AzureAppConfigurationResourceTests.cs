// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.Tests;

public class AzureAppConfigurationResourceTests
{
    [Fact]
    public void NameOutputReference_returns_expected_output_reference()
    {
        var resource = new AzureAppConfigurationResource("myappconfig", _ => { });
        var output = resource.NameOutputReference;
        Assert.Equal("name", output.Name);
        Assert.Same(resource, output.Resource);
    }

    [Fact]
    public void Endpoint_returns_expected_output_reference()
    {
        var resource = new AzureAppConfigurationResource("myappconfig", _ => { });
        var output = resource.Endpoint;
        Assert.Equal("appConfigEndpoint", output.Name);
        Assert.Same(resource, output.Resource);
    }
}
