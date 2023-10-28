// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class AddPostgresTests
{
    [Fact]
    public void AddPostgresWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddPostgresContainer("myPostgres");

        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.GetContainerResources());
        Assert.Equal("myPostgres", containerResource.Name);

        var manifestPublishing = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestPublishing.Callback);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("latest", containerAnnotation.Tag);
        Assert.Equal("postgres", containerAnnotation.Image);
        Assert.Null(containerAnnotation.Registry);

        var serviceBinding = Assert.Single(containerResource.Annotations.OfType<ServiceBindingAnnotation>());
        Assert.Equal(5432, serviceBinding.ContainerPort);
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
                Assert.Equal("POSTGRES_HOST_AUTH_METHOD", env.Key);
                Assert.Equal("trust", env.Value);
            },
            env =>
            {
                Assert.Equal("POSTGRES_PASSWORD", env.Key);
                Assert.False(string.IsNullOrEmpty(env.Value));
            });
    }

    [Fact]
    public void AddPostgresAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddPostgresContainer("myPostgres", 1234, "pass");

        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.GetContainerResources());
        Assert.Equal("myPostgres", containerResource.Name);

        var manifestPublishing = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestPublishing.Callback);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("latest", containerAnnotation.Tag);
        Assert.Equal("postgres", containerAnnotation.Image);
        Assert.Null(containerAnnotation.Registry);

        var serviceBinding = Assert.Single(containerResource.Annotations.OfType<ServiceBindingAnnotation>());
        Assert.Equal(5432, serviceBinding.ContainerPort);
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
                Assert.Equal("POSTGRES_HOST_AUTH_METHOD", env.Key);
                Assert.Equal("trust", env.Value);
            },
            env =>
            {
                Assert.Equal("POSTGRES_PASSWORD", env.Key);
                Assert.Equal("pass", env.Value);
            });
    }

    [Fact]
    public void PostgresCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddPostgresContainer("postgres")
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
        Assert.StartsWith("Host=localhost;Port=2000;Username=postgres;Password=", connectionString);
        Assert.EndsWith(";", connectionString);
    }

    [Fact]
    public void PostgresCreatesConnectionStringWithDatabase()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddPostgresContainer("postgres")
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

        var postgresResource = Assert.Single(appModel.Resources.OfType<PostgresContainerResource>());
        var postgresConnectionString = postgresResource.GetConnectionString();
        var postgresDatabaseResource = Assert.Single(appModel.Resources.OfType<PostgresDatabaseResource>());
        var dbConnectionString = postgresDatabaseResource.GetConnectionString();

        Assert.EndsWith(";", postgresConnectionString);
        Assert.Equal(postgresConnectionString + "Database=db", dbConnectionString);
    }

    [Fact]
    public void AddDatabaseToPostgresAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddPostgresContainer("postgres", 1234, "pass").AddDatabase("db");

        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var containerResources = appModel.GetContainerResources();

        var containerResource = Assert.Single(containerResources);
        Assert.Equal("postgres", containerResource.Name);

        var manifestPublishing = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestPublishing.Callback);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("latest", containerAnnotation.Tag);
        Assert.Equal("postgres", containerAnnotation.Image);
        Assert.Null(containerAnnotation.Registry);

        var serviceBinding = Assert.Single(containerResource.Annotations.OfType<ServiceBindingAnnotation>());
        Assert.Equal(5432, serviceBinding.ContainerPort);
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
                Assert.Equal("POSTGRES_HOST_AUTH_METHOD", env.Key);
                Assert.Equal("trust", env.Value);
            },
            env =>
            {
                Assert.Equal("POSTGRES_PASSWORD", env.Key);
                Assert.Equal("pass", env.Value);
            });
    }

    [Fact]
    public void AddPostgresConnectionAddsMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddPostgresConnection("myPostgres", "endpoint");

        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<IResourceWithConnectionString>());
        Assert.Equal("endpoint", connectionStringResource.GetConnectionString());
        Assert.Equal("myPostgres", connectionStringResource.Name);

        var manifestAnnotation = Assert.Single(connectionStringResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestAnnotation.Callback);
    }
}
