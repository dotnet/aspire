// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests.Oracle;

public class AddOracleDatabaseTests
{
    [Fact]
    public void AddOracleDatabaseWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddOracleDatabaseContainer("orcl");

        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.GetContainerResources());
        Assert.Equal("orcl", containerResource.Name);

        var manifestPublishing = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestPublishing.Callback);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("latest", containerAnnotation.Tag);
        Assert.Equal("database/free", containerAnnotation.Image);
        Assert.Equal("container-registry.oracle.com", containerAnnotation.Registry);

        var serviceBinding = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(1521, serviceBinding.ContainerPort);
        Assert.False(serviceBinding.IsExternal);
        Assert.Equal("tcp", serviceBinding.Name);
        Assert.Null(serviceBinding.Port);
        Assert.Equal(ProtocolType.Tcp, serviceBinding.Protocol);
        Assert.Equal("tcp", serviceBinding.Transport);
        Assert.Equal("tcp", serviceBinding.UriScheme);

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
                Assert.Equal("ORACLE_PWD", env.Key);
                Assert.False(string.IsNullOrEmpty(env.Value));
            });
    }

    [Fact]
    public void AddOracleDatabaseAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddOracleDatabaseContainer("orcl", 1234, "pass");

        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.GetContainerResources());
        Assert.Equal("orcl", containerResource.Name);

        var manifestPublishing = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestPublishing.Callback);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("latest", containerAnnotation.Tag);
        Assert.Equal("database/free", containerAnnotation.Image);
        Assert.Equal("container-registry.oracle.com", containerAnnotation.Registry);

        var serviceBinding = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(1521, serviceBinding.ContainerPort);
        Assert.False(serviceBinding.IsExternal);
        Assert.Equal("tcp", serviceBinding.Name);
        Assert.Equal(1234, serviceBinding.Port);
        Assert.Equal(ProtocolType.Tcp, serviceBinding.Protocol);
        Assert.Equal("tcp", serviceBinding.Transport);
        Assert.Equal("tcp", serviceBinding.UriScheme);

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
                Assert.Equal("ORACLE_PWD", env.Key);
                Assert.Equal("pass", env.Value);
            });
    }

    [Fact]
    public void OracleCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddOracleDatabaseContainer("orcl")
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

        Assert.StartsWith("user id=system;password=", connectionString);
        Assert.EndsWith(";data source=localhost:2000", connectionString);
    }

    [Fact]
    public void OracleCreatesConnectionStringWithDatabase()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddOracleDatabaseContainer("orcl")
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

        var oracleResource = Assert.Single(appModel.Resources.OfType<OracleDatabaseContainerResource>());
        var oracleConnectionString = oracleResource.GetConnectionString();
        var oracleDatabaseResource = Assert.Single(appModel.Resources.OfType<OracleDatabaseResource>());
        var dbConnectionString = oracleDatabaseResource.GetConnectionString();

        Assert.Equal(oracleConnectionString + "/db", dbConnectionString);
    }

    [Fact]
    public void AddDatabaseToOracleDatabaseAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddOracleDatabaseContainer("oracle", 1234, "pass").AddDatabase("db");

        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var containerResources = appModel.GetContainerResources();

        var containerResource = Assert.Single(containerResources);
        Assert.Equal("oracle", containerResource.Name);

        var manifestPublishing = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestPublishing.Callback);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("latest", containerAnnotation.Tag);
        Assert.Equal("database/free", containerAnnotation.Image);
        Assert.Equal("container-registry.oracle.com", containerAnnotation.Registry);

        var serviceBinding = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(1521, serviceBinding.ContainerPort);
        Assert.False(serviceBinding.IsExternal);
        Assert.Equal("tcp", serviceBinding.Name);
        Assert.Equal(1234, serviceBinding.Port);
        Assert.Equal(ProtocolType.Tcp, serviceBinding.Protocol);
        Assert.Equal("tcp", serviceBinding.Transport);
        Assert.Equal("tcp", serviceBinding.UriScheme);

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
                Assert.Equal("ORACLE_PWD", env.Key);
                Assert.Equal("pass", env.Value);
            });
    }
}
