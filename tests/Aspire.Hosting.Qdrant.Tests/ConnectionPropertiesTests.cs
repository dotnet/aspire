// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Qdrant.Tests;

public class ConnectionPropertiesTests
{
    [Fact]
    public void QdrantServerResourceGetConnectionPropertiesReturnsExpectedValues()
    {
    var apiKey = new ParameterResource("apiKey", _ => "p@ssw0rd1", secret: true);
        var resource = new QdrantServerResource("qdrant", apiKey);

        var properties = ((IResourceWithConnectionString)resource).GetConnectionProperties().ToArray();

        Assert.Collection(
            properties,
            property =>
            {
                Assert.Equal("GrpcHost", property.Key);
                Assert.Equal("{qdrant.bindings.grpc.host}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("GrpcPort", property.Key);
                Assert.Equal("{qdrant.bindings.grpc.port}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("HttpHost", property.Key);
                Assert.Equal("{qdrant.bindings.http.host}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("HttpPort", property.Key);
                Assert.Equal("{qdrant.bindings.http.port}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("ApiKey", property.Key);
                Assert.Equal("{apiKey.value}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("Uri", property.Key);
                Assert.Equal("{qdrant.bindings.grpc.url}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("HttpUri", property.Key);
                Assert.Equal("{qdrant.bindings.http.url}", property.Value.ValueExpression);
            });
    }
}