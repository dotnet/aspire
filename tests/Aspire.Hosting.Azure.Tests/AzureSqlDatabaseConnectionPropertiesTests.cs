// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureSqlDatabaseConnectionPropertiesTests
{
    [Fact]
    public void AzureSqlDatabaseResourceGetConnectionPropertiesReturnsExpectedValues()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var sqlServer = builder.AddAzureSqlServer("sql");
        var database = sqlServer.AddDatabase("database", "mydb");

        var resource = Assert.Single(builder.Resources.OfType<AzureSqlDatabaseResource>());
        var properties = ((IResourceWithConnectionString)resource).GetConnectionProperties().ToDictionary(x => x.Key, x => x.Value);

        // Should have parent properties (Host, Port, Uri) combined with child properties (Database, Uri-overridden, JdbcConnectionString)
        // Result: Host, Port, Database, Uri (from child, which includes /database), JdbcConnectionString = 5 properties
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
                Assert.Equal("{sql.outputs.sqlServerFqdn}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("JdbcConnectionString", property.Key);
                Assert.Equal("jdbc:sqlserver://{sql.outputs.sqlServerFqdn}:1433;database=mydb;encrypt=true;trustServerCertificate=false", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("Port", property.Key);
                Assert.Equal("1433", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("Uri", property.Key);
                Assert.Equal("mssql://{sql.outputs.sqlServerFqdn}:1433/mydb", property.Value.ValueExpression);
            });
    }
}
