// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Nats.Tests;

public class ConnectionPropertiesTests
{
    [Fact]
    public void NatsServerResourceGetConnectionPropertiesReturnsExpectedValues()
    {
        var user = new ParameterResource("user", _ => "natsuser");
    var password = new ParameterResource("password", _ => "p@ssw0rd1", secret: true);
        var resource = new NatsServerResource("nats", user, password);

        var properties = ((IResourceWithConnectionString)resource).GetConnectionProperties().ToArray();

        Assert.Collection(
            properties,
            property =>
            {
                Assert.Equal("Host", property.Key);
                Assert.Equal("{nats.bindings.tcp.host}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("Port", property.Key);
                Assert.Equal("{nats.bindings.tcp.port}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("Username", property.Key);
                Assert.Equal("{user.value}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("Password", property.Key);
                Assert.Equal("{password.value}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("Uri", property.Key);
                Assert.Equal("nats://{user.value}:{password.value}@{nats.bindings.tcp.host}:{nats.bindings.tcp.port}", property.Value.ValueExpression);
            });
    }
}