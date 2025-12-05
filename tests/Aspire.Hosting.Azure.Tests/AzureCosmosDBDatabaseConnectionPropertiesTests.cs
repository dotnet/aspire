// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureCosmosDBDatabaseConnectionPropertiesTests
{
    [Fact]
    public void AzureCosmosDBDatabaseResourceGetConnectionPropertiesReturnsExpectedValues()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var cosmosdb = builder.AddAzureCosmosDB("cosmosdb");
        var database = cosmosdb.AddCosmosDatabase("database", "mydb");

        var resource = Assert.Single(builder.Resources.OfType<AzureCosmosDBDatabaseResource>());
        var properties = ((IResourceWithConnectionString)resource).GetConnectionProperties().ToDictionary(x => x.Key, x => x.Value);

        // Should have parent properties (Uri, AccountKey) + Database
        Assert.Equal(3, properties.Count);
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
            });
    }

    [Fact]
    public void AzureCosmosDBDatabaseResourceWithAccessKeyAuthenticationGetConnectionPropertiesReturnsExpectedValues()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var cosmosdb = builder.AddAzureCosmosDB("cosmosdb").WithAccessKeyAuthentication();
        var database = cosmosdb.AddCosmosDatabase("database", "mydb");

        var resource = Assert.Single(builder.Resources.OfType<AzureCosmosDBDatabaseResource>());
        var properties = ((IResourceWithConnectionString)resource).GetConnectionProperties().ToDictionary(x => x.Key, x => x.Value);

        // Should have parent properties (Uri, AccountKey) + Database
        Assert.Equal(3, properties.Count);
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
            });
    }
}
