// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Seq.Tests;

public class ConnectionPropertiesTests
{
    [Fact]
    public void SeqResourceGetConnectionPropertiesReturnsExpectedValues()
    {
        var resource = new SeqResource("seq");

        var properties = ((IResourceWithConnectionString)resource).GetConnectionProperties().ToArray();

        Assert.Collection(
            properties,
            property =>
            {
                Assert.Equal("Host", property.Key);
                Assert.Equal("{seq.bindings.http.host}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("Port", property.Key);
                Assert.Equal("{seq.bindings.http.port}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("Uri", property.Key);
                Assert.Equal("{seq.bindings.http.url}", property.Value.ValueExpression);
            });
    }
}