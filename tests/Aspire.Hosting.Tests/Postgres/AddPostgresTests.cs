// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json;
using Aspire.Hosting.Postgres;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests.Postgres;

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

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(5432, endpoint.ContainerPort);
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
                Assert.Equal("POSTGRES_HOST_AUTH_METHOD", env.Key);
                Assert.Equal("scram-sha-256", env.Value);
            },
            env =>
            {
                Assert.Equal("POSTGRES_INITDB_ARGS", env.Key);
                Assert.Equal("--auth-host=scram-sha-256 --auth-local=scram-sha-256", env.Value);
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

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(5432, endpoint.ContainerPort);
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
                Assert.Equal("POSTGRES_HOST_AUTH_METHOD", env.Key);
                Assert.Equal("scram-sha-256", env.Value);
            },
            env =>
            {
                Assert.Equal("POSTGRES_INITDB_ARGS", env.Key);
                Assert.Equal("--auth-host=scram-sha-256 --auth-local=scram-sha-256", env.Value);
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

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(5432, endpoint.ContainerPort);
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
                Assert.Equal("POSTGRES_HOST_AUTH_METHOD", env.Key);
                Assert.Equal("scram-sha-256", env.Value);
            },
            env =>
            {
                Assert.Equal("POSTGRES_INITDB_ARGS", env.Key);
                Assert.Equal("--auth-host=scram-sha-256 --auth-local=scram-sha-256", env.Value);
            },
            env =>
            {
                Assert.Equal("POSTGRES_PASSWORD", env.Key);
                Assert.Equal("pass", env.Value);
            });
    }

    [Fact]
    public void WithPgAdminAddsContainer()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddPostgres("mypostgres").WithPgAdmin(8081);

        var container = builder.Resources.Single(r => r.Name == "mypostgres-pgadmin");
        var volume = container.Annotations.OfType<VolumeMountAnnotation>().Single();

        Assert.True(File.Exists(volume.Source)); // File should exist, but will be empty.
        Assert.Equal("/pgadmin4/servers.json", volume.Target);
    }

    [Fact]
    public void WithPostgresTwiceEndsUpWithOneContainer()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddPostgres("mypostgres1").WithPgAdmin(8081);
        builder.AddPostgres("mypostgres2").WithPgAdmin(8081);

        builder.Resources.Single(r => r.Name.EndsWith("-pgadmin"));
    }

    [Fact]
    public void WithPostgresProducesValidServersJsonFile()
    {
        var builder = DistributedApplication.CreateBuilder();
        var pg1 = builder.AddPostgres("mypostgres1").WithPgAdmin(8081);
        var pg2 = builder.AddPostgres("mypostgres2").WithPgAdmin(8081);

        // Add fake allocated endpoints.
        pg1.WithAnnotation(new AllocatedEndpointAnnotation("tcp", ProtocolType.Tcp, "host.docker.internal", 5001, "tcp"));
        pg2.WithAnnotation(new AllocatedEndpointAnnotation("tcp", ProtocolType.Tcp, "host.docker.internal", 5002, "tcp"));

        var pgadmin = builder.Resources.Single(r => r.Name.EndsWith("-pgadmin"));
        var volume = pgadmin.Annotations.OfType<VolumeMountAnnotation>().Single();

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var hook = new PgAdminConfigWriterHook();
        hook.AfterEndpointsAllocatedAsync(appModel, CancellationToken.None);

        using var stream = File.OpenRead(volume.Source);
        var document = JsonDocument.Parse(stream);

        var servers = document.RootElement.GetProperty("Servers");

        // Make sure the first server is correct.
        Assert.Equal(pg1.Resource.Name, servers.GetProperty("1").GetProperty("Name").GetString());
        Assert.Equal("Aspire instances", servers.GetProperty("1").GetProperty("Group").GetString());
        Assert.Equal("host.docker.internal", servers.GetProperty("1").GetProperty("Host").GetString());
        Assert.Equal(5001, servers.GetProperty("1").GetProperty("Port").GetInt32());
        Assert.Equal("postgres", servers.GetProperty("1").GetProperty("Username").GetString());
        Assert.Equal("prefer", servers.GetProperty("1").GetProperty("SSLMode").GetString());
        Assert.Equal("postgres", servers.GetProperty("1").GetProperty("MaintenanceDB").GetString());
        Assert.Equal($"echo '{pg1.Resource.Password}'", servers.GetProperty("1").GetProperty("PasswordExecCommand").GetString());

        // Make sure the second server is correct.
        Assert.Equal(pg2.Resource.Name, servers.GetProperty("2").GetProperty("Name").GetString());
        Assert.Equal("Aspire instances", servers.GetProperty("2").GetProperty("Group").GetString());
        Assert.Equal("host.docker.internal", servers.GetProperty("2").GetProperty("Host").GetString());
        Assert.Equal(5002, servers.GetProperty("2").GetProperty("Port").GetInt32());
        Assert.Equal("postgres", servers.GetProperty("2").GetProperty("Username").GetString());
        Assert.Equal("prefer", servers.GetProperty("2").GetProperty("SSLMode").GetString());
        Assert.Equal("postgres", servers.GetProperty("2").GetProperty("MaintenanceDB").GetString());
        Assert.Equal($"echo '{pg2.Resource.Password}'", servers.GetProperty("2").GetProperty("PasswordExecCommand").GetString());
    }
}
