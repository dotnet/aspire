// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureServiceBusConnectionPropertiesTests
{
    [Fact]
    public void AzureServiceBusResourceGetConnectionPropertiesReturnsExpectedValues()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var serviceBus = builder.AddAzureServiceBus("servicebus");

        var properties = ((IResourceWithConnectionString)serviceBus.Resource).GetConnectionProperties().ToArray();

        Assert.Collection(
            properties,
            property =>
            {
                Assert.Equal("Host", property.Key);
                Assert.Equal("{servicebus.outputs.serviceBusEndpoint}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("Uri", property.Key);
                Assert.Equal("sb://{servicebus.outputs.serviceBusEndpoint}", property.Value.ValueExpression);
            });
    }
}
