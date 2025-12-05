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

        // Should have grandparent properties (Uri, AccountKey) + parent Database + ContainerName
        Assert.Equal(4, properties.Count);
        Assert.Collection(
            properties,
            property =>
            {
                Assert.Equal("Uri", property.Key);
                Assert.Equal("{cosmosdb.outputs.accountEndpoint}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("AccountKey", property.Key);
                Assert.Equal("", property.Value.ValueExpression);
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

    [Fact]
    public void AzureCosmosDBContainerResourceWithAccessKeyAuthenticationGetConnectionPropertiesReturnsExpectedValues()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var cosmosdb = builder.AddAzureCosmosDB("cosmosdb").WithAccessKeyAuthentication();
        var database = cosmosdb.AddCosmosDatabase("database", "mydb");
        var container = database.AddContainer("container", "/id", "mycontainer");

        var resource = Assert.Single(builder.Resources.OfType<AzureCosmosDBContainerResource>());
        var properties = ((IResourceWithConnectionString)resource).GetConnectionProperties().ToDictionary(x => x.Key, x => x.Value);

        // Should have grandparent properties (Uri, AccountKey) + parent Database + ContainerName
        Assert.Equal(4, properties.Count);
        Assert.Collection(
            properties,
            property =>
            {
                Assert.Equal("Uri", property.Key);
                Assert.Equal("{cosmosdb.outputs.accountEndpoint}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("AccountKey", property.Key);
                Assert.Equal("{cosmosdb-kv.secrets.primaryaccesskey--cosmosdb}", property.Value.ValueExpression);
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
