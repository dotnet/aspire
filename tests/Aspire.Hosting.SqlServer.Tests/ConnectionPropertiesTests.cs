// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.SqlServer.Tests;

public class ConnectionPropertiesTests
{
    [Fact]
    public void SqlServerServerResourceGetConnectionPropertiesReturnsExpectedValues()
    {
    var password = new ParameterResource("password", _ => "p@ssw0rd1", secret: true);
        var resource = new SqlServerServerResource("sql", password);

        var properties = ((IResourceWithConnectionString)resource).GetConnectionProperties().ToArray();

        Assert.Collection(
            properties,
            property =>
            {
                Assert.Equal("Host", property.Key);
                Assert.Equal("{sql.bindings.tcp.host}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("Port", property.Key);
                Assert.Equal("{sql.bindings.tcp.port}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("Username", property.Key);
                Assert.Equal("sa", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("Password", property.Key);
                Assert.Equal("{password.value}", property.Value.ValueExpression);
            },
            property =>
            {
                Assert.Equal("JdbcConnectionString", property.Key);
                Assert.Equal("jdbc:sqlserver://{sql.bindings.tcp.host}:{sql.bindings.tcp.port};user=sa;password={password.value};trustServerCertificate=true", property.Value.ValueExpression);
            });
    }

    [Fact]
    public void SqlServerDatabaseResourceGetConnectionPropertiesIncludesDatabaseSpecificValues()
    {
    var password = new ParameterResource("password", _ => "p@ssw0rd1", secret: true);
        var server = new SqlServerServerResource("sql", password);
        var resource = new SqlServerDatabaseResource("sqlDb", "Orders", server);

        var properties = ((IResourceWithConnectionString)resource).GetConnectionProperties().ToArray();

        Assert.Contains(properties, property => property.Key == "Host" && property.Value.ValueExpression == "{sql.bindings.tcp.host}");
        Assert.Contains(properties, property => property.Key == "Port" && property.Value.ValueExpression == "{sql.bindings.tcp.port}");
        Assert.Contains(properties, property => property.Key == "Username" && property.Value.ValueExpression == "sa");
        Assert.Contains(properties, property => property.Key == "Password" && property.Value.ValueExpression == "{password.value}");
        Assert.Contains(properties, property => property.Key == "Database" && property.Value.ValueExpression == "Orders");
        Assert.Contains(
            properties,
            property => property.Key == "JdbcConnectionString" &&
                        property.Value.ValueExpression == "jdbc:sqlserver://{sql.bindings.tcp.host}:{sql.bindings.tcp.port};user=sa;password={password.value};databaseName=Orders;trustServerCertificate=true");
    }
}