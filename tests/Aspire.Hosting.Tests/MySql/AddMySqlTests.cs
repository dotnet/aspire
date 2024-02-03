// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests.MySql;

public class AddMySqlTests
{
    [Fact]
    public void AddMySqlContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddMySqlContainer("mysql");

        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<MySqlContainerResource>());
        Assert.Equal("mysql", containerResource.Name);

        var manifestAnnotation = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestAnnotation.Callback);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("latest", containerAnnotation.Tag);
        Assert.Equal("mysql", containerAnnotation.Image);
        Assert.Null(containerAnnotation.Registry);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(3306, endpoint.ContainerPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Null(endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var envAnnotations = containerResource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext("dcp", config);

        foreach (var annotation in envAnnotations)
        {
            annotation.Callback(context);
        }

        Assert.Collection(config,
            env =>
            {
                Assert.Equal("MYSQL_ROOT_PASSWORD", env.Key);
                Assert.False(string.IsNullOrEmpty(env.Value));
            });
    }

    [Fact]
    public void AddMySqlAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddMySqlContainer("mysql", 1234, "pass");

        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.GetContainerResources());
        Assert.Equal("mysql", containerResource.Name);

        var manifestPublishing = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestPublishing.Callback);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("latest", containerAnnotation.Tag);
        Assert.Equal("mysql", containerAnnotation.Image);
        Assert.Null(containerAnnotation.Registry);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(3306, endpoint.ContainerPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Equal(1234, endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var envAnnotations = containerResource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext("dcp", config);

        foreach (var annotation in envAnnotations)
        {
            annotation.Callback(context);
        }

        Assert.Collection(config,
            env =>
            {
                Assert.Equal("MYSQL_ROOT_PASSWORD", env.Key);
                Assert.Equal("pass", env.Value);
            });
    }

    [Fact]
    public void MySqlCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddMySqlContainer("mysql")
            .WithAnnotation(
            new AllocatedEndpointAnnotation("mybinding",
            ProtocolType.Tcp,
            "localhost",
            2000,
            "https"
            ));

        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<IResourceWithConnectionString>());
        var connectionString = connectionStringResource.GetConnectionString();
        Assert.StartsWith("Server=localhost;Port=2000;User ID=root;Password=", connectionString);
        Assert.EndsWith(";", connectionString);
    }

    [Fact]
    public void MySqlCreatesConnectionStringWithDatabase()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddMySqlContainer("mysql")
            .WithAnnotation(
            new AllocatedEndpointAnnotation("mybinding",
            ProtocolType.Tcp,
            "localhost",
            2000,
            "https"
            ))
            .AddDatabase("db");

        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var mySqlResource = Assert.Single(appModel.Resources.OfType<MySqlContainerResource>());
        var mySqlConnectionString = mySqlResource.GetConnectionString();
        var mySqlDatabaseResource = Assert.Single(appModel.Resources.OfType<MySqlDatabaseResource>());
        var dbConnectionString = mySqlDatabaseResource.GetConnectionString();

        Assert.EndsWith(";", mySqlConnectionString);
        Assert.Equal(mySqlConnectionString + "Database=db", dbConnectionString);
    }
}
