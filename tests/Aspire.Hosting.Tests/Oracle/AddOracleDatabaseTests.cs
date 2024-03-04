// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests.Oracle;

public class AddOracleDatabaseTests
{
    [Fact]
    public async Task AddOracleDatabaseWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddOracleDatabase("orcl");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.GetContainerResources());
        Assert.Equal("orcl", containerResource.Name);

        var manifestPublishing = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestPublishing.Callback);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("23.3.0.0", containerAnnotation.Tag);
        Assert.Equal("database/free", containerAnnotation.Image);
        Assert.Equal("container-registry.oracle.com", containerAnnotation.Registry);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(1521, endpoint.ContainerPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Null(endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(containerResource);

        Assert.Collection(config,
            env =>
            {
                Assert.Equal("ORACLE_PWD", env.Key);
                Assert.False(string.IsNullOrEmpty(env.Value));
            });
    }

    [Fact]
    public async Task AddOracleDatabaseAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddOracleDatabase("orcl", 1234, "pass");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.GetContainerResources());
        Assert.Equal("orcl", containerResource.Name);

        var manifestPublishing = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestPublishing.Callback);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("23.3.0.0", containerAnnotation.Tag);
        Assert.Equal("database/free", containerAnnotation.Image);
        Assert.Equal("container-registry.oracle.com", containerAnnotation.Registry);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(1521, endpoint.ContainerPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Equal(1234, endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(containerResource);

        Assert.Collection(config,
            env =>
            {
                Assert.Equal("ORACLE_PWD", env.Key);
                Assert.Equal("pass", env.Value);
            });
    }

    [Fact]
    public void OracleCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddOracleDatabase("orcl")
            .WithAnnotation(
            new AllocatedEndpointAnnotation(OracleDatabaseServerResource.PrimaryEndpointName,
            ProtocolType.Tcp,
            "localhost",
            2000,
            "https"
            ));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<IResourceWithConnectionString>());
        var connectionString = connectionStringResource.GetConnectionString();

        Assert.Equal("user id=system;password={orcl.inputs.password};data source={orcl.bindings.tcp.host}:{orcl.bindings.tcp.port};", connectionStringResource.ConnectionStringExpression);
        Assert.StartsWith("user id=system;password=", connectionString);
        Assert.EndsWith(";data source=localhost:2000", connectionString);
    }

    [Fact]
    public void OracleCreatesConnectionStringWithDatabase()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddOracleDatabase("orcl")
            .WithAnnotation(
            new AllocatedEndpointAnnotation(OracleDatabaseServerResource.PrimaryEndpointName,
            ProtocolType.Tcp,
            "localhost",
            2000,
            "https"
            ))
            .AddDatabase("db");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var oracleResource = Assert.Single(appModel.Resources.OfType<OracleDatabaseServerResource>());
        var oracleConnectionString = oracleResource.GetConnectionString();
        var oracleDatabaseResource = Assert.Single(appModel.Resources.OfType<OracleDatabaseResource>());
        var dbConnectionString = oracleDatabaseResource.GetConnectionString();

        Assert.Equal("{orcl.connectionString}/db", oracleDatabaseResource.ConnectionStringExpression);
        Assert.Equal(oracleConnectionString + "/db", dbConnectionString);
    }

    [Fact]
    public async Task AddDatabaseToOracleDatabaseAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddOracleDatabase("oracle", 1234, "pass").AddDatabase("db");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var containerResources = appModel.GetContainerResources();

        var containerResource = Assert.Single(containerResources);
        Assert.Equal("oracle", containerResource.Name);

        var manifestPublishing = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestPublishing.Callback);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("23.3.0.0", containerAnnotation.Tag);
        Assert.Equal("database/free", containerAnnotation.Image);
        Assert.Equal("container-registry.oracle.com", containerAnnotation.Registry);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(1521, endpoint.ContainerPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Equal(1234, endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(containerResource);

        Assert.Collection(config,
            env =>
            {
                Assert.Equal("ORACLE_PWD", env.Key);
                Assert.Equal("pass", env.Value);
            });
    }

    [Fact]
    public async Task VerifyManifest()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var oracleServer = appBuilder.AddOracleDatabase("oracle");
        var db = oracleServer.AddDatabase("db");

        var serverManifest = await ManifestUtils.GetManifest(oracleServer.Resource);
        var dbManifest = await ManifestUtils.GetManifest(db.Resource);

        var expectedManifest = """
            {
              "type": "container.v0",
              "connectionString": "user id=system;password={oracle.inputs.password};data source={oracle.bindings.tcp.host}:{oracle.bindings.tcp.port};",
              "image": "container-registry.oracle.com/database/free:23.3.0.0",
              "env": {
                "ORACLE_PWD": "{oracle.inputs.password}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "containerPort": 1521
                }
              },
              "inputs": {
                "password": {
                  "type": "string",
                  "secret": true,
                  "default": {
                    "generate": {
                      "minLength": 10
                    }
                  }
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, serverManifest.ToString());

        expectedManifest = """
            {
              "type": "value.v0",
              "connectionString": "{oracle.connectionString}/db"
            }
            """;
        Assert.Equal(expectedManifest, dbManifest.ToString());
    }

    [Fact]
    public void ThrowsWithIdenticalChildResourceNames()
    {
        var builder = DistributedApplication.CreateBuilder();

        var db = builder.AddOracleDatabase("oracle1");
        db.AddDatabase("db");

        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [Fact]
    public void ThrowsWithIdenticalChildResourceNamesDifferentParents()
    {
        var builder = DistributedApplication.CreateBuilder();

        builder.AddOracleDatabase("oracle1")
            .AddDatabase("db");

        var db = builder.AddOracleDatabase("oracle2");
        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [Fact]
    public void CanAddDatabasesWithDifferentNamesOnSingleServer()
    {
        var builder = DistributedApplication.CreateBuilder();

        var oracle1 = builder.AddOracleDatabase("oracle1");

        var db1 = oracle1.AddDatabase("db1", "customers1");
        var db2 = oracle1.AddDatabase("db2", "customers2");

        Assert.Equal("customers1", db1.Resource.DatabaseName);
        Assert.Equal("customers2", db2.Resource.DatabaseName);

        Assert.Equal("{oracle1.connectionString}/customers1", db1.Resource.ConnectionStringExpression);
        Assert.Equal("{oracle1.connectionString}/customers2", db2.Resource.ConnectionStringExpression);
    }

    [Fact]
    public void CanAddDatabasesWithTheSameNameOnMultipleServers()
    {
        var builder = DistributedApplication.CreateBuilder();

        var db1 = builder.AddOracleDatabase("oracle1")
            .AddDatabase("db1", "imports");

        var db2 = builder.AddOracleDatabase("oracle2")
            .AddDatabase("db2", "imports");

        Assert.Equal("imports", db1.Resource.DatabaseName);
        Assert.Equal("imports", db2.Resource.DatabaseName);

        Assert.Equal("{oracle1.connectionString}/imports", db1.Resource.ConnectionStringExpression);
        Assert.Equal("{oracle2.connectionString}/imports", db2.Resource.ConnectionStringExpression);
    }
}
