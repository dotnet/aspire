// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests;

public class AzurePostgresFlexibleServerDatabaseConnectionPropertiesTests
{
    [Fact]
    public void AzurePostgresFlexibleServerDatabaseResourceGetConnectionPropertiesReturnsExpectedValues()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var postgres = builder.AddAzurePostgresFlexibleServer("postgres");
        var database = postgres.AddDatabase("database", "mydb");

        var resource = Assert.Single(builder.Resources.OfType<AzurePostgresFlexibleServerDatabaseResource>());
        var properties = ((IResourceWithConnectionString)resource).GetConnectionProperties().ToDictionary(x => x.Key, x => x.Value);

        // Should have 5 properties: Database, Host, Port, Uri, JdbcConnectionString
        // Note: AzurePostgresFlexibleServerResource does not include Username/Password
        Assert.Equal(5, properties.Count);
        Assert.Collection(
            properties.OrderBy(p => p.Key),
            property =>
            {
                Assert.Equal("Database", property.Key);
                Assert.Equal("mydb", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("Host", property.Key);
                Assert.Equal("{postgres.outputs.hostName}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("JdbcConnectionString", property.Key);
                Assert.StartsWith("jdbc:postgresql://{postgres.outputs.hostName}/mydb", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("Port", property.Key);
                Assert.Equal("5432", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("Uri", property.Key);
                Assert.StartsWith("postgresql://", property.Value.ValueExpression);
            });
    }
}
