// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureCosmosDBContainerConnectionPropertiesTests
{
    [Fact]
    public void AzureCosmosDBContainerResourceGetConnectionPropertiesReturnsExpectedValues()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var cosmosdb = builder.AddAzureCosmosDB("cosmosdb");
        var database = cosmosdb.AddCosmosDatabase("database", "mydb");
        var container = database.AddContainer("container", "/id", "mycontainer");

        var resource = Assert.Single(builder.Resources.OfType<AzureCosmosDBContainerResource>());
        var properties = ((IResourceWithConnectionString)resource).GetConnectionProperties().ToDictionary(x => x.Key, x => x.Value);

        // Should have grandparent properties + parent Database + ContainerName
        Assert.Equal(3, properties.Count);
        Assert.Collection(
            properties,
            property =>
            {
                Assert.Equal("Uri", property.Key);
                Assert.Equal("{cosmosdb.outputs.connectionString}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("Database", property.Key);
                Assert.Equal("mydb", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("ContainerName", property.Key);
                Assert.Equal("mycontainer", property.Value.ValueExpression);
            });
    }
}
