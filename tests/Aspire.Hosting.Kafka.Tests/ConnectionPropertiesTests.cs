// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Kafka.Tests;

public class ConnectionPropertiesTests
{
    [Fact]
    public void KafkaServerResourceGetConnectionPropertiesReturnsExpectedValues()
    {
        var resource = new KafkaServerResource("kafka");

        var properties = ((IResourceWithConnectionString)resource).GetConnectionProperties().ToArray();

        Assert.Collection(
            properties,
            property =>
            {
                Assert.Equal("Host", property.Key);
                Assert.Equal("{kafka.bindings.tcp.host}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("Port", property.Key);
                Assert.Equal("{kafka.bindings.tcp.port}", property.Value.ValueExpression);
            });
    }
}