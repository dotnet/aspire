// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Sockets;
using Xunit;

namespace Aspire.Hosting.Tests.SqlServer;

public class AddSqlServerTests
{
    [Fact]
    public void AddSqlServerContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddSqlServer("sqlserver");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<SqlServerServerResource>());
        Assert.Equal("sqlserver", containerResource.Name);

        var manifestAnnotation = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestAnnotation.Callback);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(1433, endpoint.ContainerPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Null(endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("2022-latest", containerAnnotation.Tag);
        Assert.Equal("mssql/server", containerAnnotation.Image);
        Assert.Equal("mcr.microsoft.com", containerAnnotation.Registry);

        var envAnnotations = containerResource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var context = new EnvironmentCallbackContext(executionContext, config);

        foreach (var annotation in envAnnotations)
        {
            annotation.Callback(context);
        }

        Assert.Collection(config,
            env =>
            {
                Assert.Equal("ACCEPT_EULA", env.Key);
                Assert.Equal("Y", env.Value);
            },
            env =>
            {
                Assert.Equal("MSSQL_SA_PASSWORD", env.Key);
                Assert.NotNull(env.Value);
                Assert.True(env.Value.Length >= 8);
            });
    }

    [Fact]
    public void SqlServerCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder
            .AddSqlServer("sqlserver")
            .WithAnnotation(
                    new AllocatedEndpointAnnotation("mybinding",
                    ProtocolType.Tcp,
                    "localhost",
                    1433,
                    "tcp"
             ));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<SqlServerServerResource>());
        var connectionString = connectionStringResource.GetConnectionString();
        var password = PasswordUtil.EscapePassword(connectionStringResource.Password);

        Assert.Equal($"Server=127.0.0.1,1433;User ID=sa;Password={password};TrustServerCertificate=true", connectionString);
        Assert.Equal("Server={sqlserver.bindings.tcp.host},{sqlserver.bindings.tcp.port};User ID=sa;Password={sqlserver.inputs.password};TrustServerCertificate=true", connectionStringResource.ConnectionStringExpression);
    }

    [Fact]
    public void SqlServerDatabaseCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder
            .AddSqlServer("sqlserver")
            .WithAnnotation(
                    new AllocatedEndpointAnnotation("mybinding",
                    ProtocolType.Tcp,
                    "localhost",
                    1433,
                    "tcp"
             )).AddDatabase("mydb");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<SqlServerDatabaseResource>());
        var connectionString = connectionStringResource.GetConnectionString();
        var password = PasswordUtil.EscapePassword(connectionStringResource.Parent.Password);

        Assert.Equal($"Server=127.0.0.1,1433;User ID=sa;Password={password};TrustServerCertificate=true;Database=mydb", connectionString);
        Assert.Equal("{sqlserver.connectionString};Database=mydb", connectionStringResource.ConnectionStringExpression);
    }

    [Fact]
    public void VerifyManifest()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var sqlServer = appBuilder.AddSqlServer("sqlserver");
        var db = sqlServer.AddDatabase("db");

        var serverManifest = ManifestUtils.GetManifest(sqlServer.Resource);
        var dbManifest = ManifestUtils.GetManifest(db.Resource);

        Assert.Equal("container.v0", serverManifest["type"]?.ToString());
        Assert.Equal(sqlServer.Resource.ConnectionStringExpression, serverManifest["connectionString"]?.ToString());

        Assert.Equal("value.v0", dbManifest["type"]?.ToString());
        Assert.Equal(db.Resource.ConnectionStringExpression, dbManifest["connectionString"]?.ToString());
    }

    [Fact]
    public void ThrowsWithIdenticalChildResourceNames()
    {
        var builder = DistributedApplication.CreateBuilder();

        var db = builder.AddSqlServer("sqlserver1");
        db.AddDatabase("db");

        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [Fact]
    public void ThrowsWithIdenticalChildResourceNamesDifferentParents()
    {
        var builder = DistributedApplication.CreateBuilder();

        builder.AddSqlServer("sqlserver1")
            .AddDatabase("db");

        var db = builder.AddSqlServer("sqlserver2");
        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [Fact]
    public void CanAddDatabasesWithDifferentNamesOnSingleServer()
    {
        var builder = DistributedApplication.CreateBuilder();

        var sqlserver1 = builder.AddSqlServer("sqlserver1");

        var db1 = sqlserver1.AddDatabase("db1", "customers1");
        var db2 = sqlserver1.AddDatabase("db2", "customers2");

        Assert.Equal("customers1", db1.Resource.DatabaseName);
        Assert.Equal("customers2", db2.Resource.DatabaseName);

        Assert.Equal("{sqlserver1.connectionString};Database=customers1", db1.Resource.ConnectionStringExpression);
        Assert.Equal("{sqlserver1.connectionString};Database=customers2", db2.Resource.ConnectionStringExpression);
    }

    [Fact]
    public void CanAddDatabasesWithTheSameNameOnMultipleServers()
    {
        var builder = DistributedApplication.CreateBuilder();

        var db1 = builder.AddPostgres("sqlserver1")
            .AddDatabase("db1", "imports");

        var db2 = builder.AddPostgres("sqlserver2")
            .AddDatabase("db2", "imports");

        Assert.Equal("imports", db1.Resource.DatabaseName);
        Assert.Equal("imports", db2.Resource.DatabaseName);

        Assert.Equal("{sqlserver1.connectionString};Database=imports", db1.Resource.ConnectionStringExpression);
        Assert.Equal("{sqlserver2.connectionString};Database=imports", db2.Resource.ConnectionStringExpression);
    }
}
