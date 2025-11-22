// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.Tests;

public class AzureCosmosDBConnectionPropertiesTests
{
    [Fact]
    public void AzureCosmosDBResourceGetConnectionPropertiesReturnsExpectedValues()
    {
        var cosmosDBResource = new AzureCosmosDBResource("cosmos", _ => { });

        var properties = ((IResourceWithConnectionString)cosmosDBResource).GetConnectionProperties().ToArray();

        Assert.Single(properties);
        Assert.Collection(
            properties,
            property =>
            {
                Assert.Equal("Uri", property.Key);
                Assert.Equal("{cosmos.outputs.connectionString}", property.Value.ValueExpression);
            });
    }
}
