// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureEventHubsConnectionPropertiesTests
{
    [Fact]
    public void AzureEventHubsResourceGetConnectionPropertiesReturnsExpectedValues()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var eventHubs = builder.AddAzureEventHubs("eventhubs");

        var properties = ((IResourceWithConnectionString)eventHubs.Resource).GetConnectionProperties().ToArray();

        Assert.Equal(2, properties.Length);
        Assert.Collection(
            properties,
            property =>
            {
                Assert.Equal("Host", property.Key);
                Assert.Equal("{eventhubs.outputs.eventHubsEndpoint}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("Uri", property.Key);
                Assert.Equal("{eventhubs.outputs.eventHubsEndpoint}", property.Value.ValueExpression);
            });
    }
}
