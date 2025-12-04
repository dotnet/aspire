// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureRedisConnectionPropertiesTests
{
    [Fact]
    public void AzureManagedRedisResourceGetConnectionPropertiesReturnsExpectedValues()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddAzureManagedRedis("redis");

        var properties = ((IResourceWithConnectionString)redis.Resource).GetConnectionProperties().ToArray();

        Assert.Equal(3, properties.Length);
        Assert.Collection(
            properties,
            property =>
            {
                Assert.Equal("Host", property.Key);
                Assert.Equal("{redis.outputs.hostName}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("Port", property.Key);
                Assert.Equal("10000", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("Uri", property.Key);
                Assert.Equal("redis://{redis.outputs.hostName}:10000", property.Value.ValueExpression);
            });
    }
}
