// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json;
using Aspire.Hosting.Postgres;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests.Postgres;

public class AddPostgresTests
{
    [Fact]
    public void AddPostgresWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddPostgres("myPostgres");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.GetContainerResources());
        Assert.Equal("myPostgres", containerResource.Name);

        var manifestPublishing = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestPublishing.Callback);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("16.2", containerAnnotation.Tag);
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
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var context = new EnvironmentCallbackContext(executionContext, config);

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
        appBuilder.AddPostgres("myPostgres", 1234, "pass");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.GetContainerResources());
        Assert.Equal("myPostgres", containerResource.Name);

        var manifestPublishing = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestPublishing.Callback);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("16.2", containerAnnotation.Tag);
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
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var context = new EnvironmentCallbackContext(executionContext, config);

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
        var postgres = appBuilder.AddPostgres("postgres")
                                 .WithAnnotation(
                                     new AllocatedEndpointAnnotation("mybinding",
                                      ProtocolType.Tcp,
                                     "localhost",
                                     2000,
                                     "https"
                                 ));

        var connectionString = postgres.Resource.GetConnectionString();
        Assert.Equal("Host={postgres.bindings.tcp.host};Port={postgres.bindings.tcp.port};Username=postgres;Password={postgres.inputs.password}", postgres.Resource.ConnectionStringExpression);
        Assert.Equal($"Host=localhost;Port=2000;Username=postgres;Password={PasswordUtil.EscapePassword(postgres.Resource.Password)}", connectionString);
    }

    [Fact]
    public void PostgresCreatesConnectionStringWithDatabase()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddPostgres("postgres")
            .WithAnnotation(
            new AllocatedEndpointAnnotation("mybinding",
            ProtocolType.Tcp,
            "localhost",
            2000,
            "https"
            ))
            .AddDatabase("db");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var postgresResource = Assert.Single(appModel.Resources.OfType<PostgresServerResource>());
        var postgresConnectionString = postgresResource.GetConnectionString();
        var postgresDatabaseResource = Assert.Single(appModel.Resources.OfType<PostgresDatabaseResource>());
        var dbConnectionString = postgresDatabaseResource.GetConnectionString();

        Assert.Equal("{postgres.connectionString};Database=db", postgresDatabaseResource.ConnectionStringExpression);
        Assert.Equal(postgresConnectionString + ";Database=db", dbConnectionString);
    }

    [Fact]
    public void AddDatabaseToPostgresAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddPostgres("postgres", 1234, "pass").AddDatabase("db");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var containerResources = appModel.GetContainerResources();

        var containerResource = Assert.Single(containerResources);
        Assert.Equal("postgres", containerResource.Name);

        var manifestPublishing = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestPublishing.Callback);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("16.2", containerAnnotation.Tag);
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
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var context = new EnvironmentCallbackContext(executionContext, config);

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
    public void VerifyManifest()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var pgServer = appBuilder.AddPostgres("pg");
        var db = pgServer.AddDatabase("db");

        var serverManifest = ManifestUtils.GetManifest(pgServer.Resource);
        var dbManifest = ManifestUtils.GetManifest(db.Resource);

        Assert.Equal("container.v0", serverManifest["type"]?.ToString());
        Assert.Equal(pgServer.Resource.ConnectionStringExpression, serverManifest["connectionString"]?.ToString());

        Assert.Equal("value.v0", dbManifest["type"]?.ToString());
        Assert.Equal(db.Resource.ConnectionStringExpression, dbManifest["connectionString"]?.ToString());
    }

    [Fact]
    public void WithPgAdminAddsContainer()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddPostgres("mypostgres").WithPgAdmin(8081);

        var container = builder.Resources.Single(r => r.Name == "mypostgres-pgadmin");
        var volume = container.Annotations.OfType<ContainerMountAnnotation>().Single();

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
        var volume = pgadmin.Annotations.OfType<ContainerMountAnnotation>().Single();

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var hook = new PgAdminConfigWriterHook();
        hook.AfterEndpointsAllocatedAsync(appModel, CancellationToken.None);

        using var stream = File.OpenRead(volume.Source!);
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

    [Fact]
    public void ThrowsWithIdenticalChildResourceNames()
    {
        var builder = DistributedApplication.CreateBuilder();

        var db = builder.AddPostgres("postgres1");
        db.AddDatabase("db");

        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [Fact]
    public void ThrowsWithIdenticalChildResourceNamesDifferentParents()
    {
        var builder = DistributedApplication.CreateBuilder();

        builder.AddPostgres("postgres1")
            .AddDatabase("db");

        var db = builder.AddPostgres("postgres2");
        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [Fact]
    public void CanAddDatabasesWithDifferentNamesOnSingleServer()
    {
        var builder = DistributedApplication.CreateBuilder();

        var postgres1 = builder.AddPostgres("postgres1");

        var db1 = postgres1.AddDatabase("db1", "customers1");
        var db2 = postgres1.AddDatabase("db2", "customers2");

        Assert.Equal("customers1", db1.Resource.DatabaseName);
        Assert.Equal("customers2", db2.Resource.DatabaseName);

        Assert.Equal("{postgres1.connectionString};Database=customers1", db1.Resource.ConnectionStringExpression);
        Assert.Equal("{postgres1.connectionString};Database=customers2", db2.Resource.ConnectionStringExpression);
    }

    [Fact]
    public void CanAddDatabasesWithTheSameNameOnMultipleServers()
    {
        var builder = DistributedApplication.CreateBuilder();

        var db1 = builder.AddPostgres("postgres1")
            .AddDatabase("db1", "imports");

        var db2 = builder.AddPostgres("postgres2")
            .AddDatabase("db2", "imports");

        Assert.Equal("imports", db1.Resource.DatabaseName);
        Assert.Equal("imports", db2.Resource.DatabaseName);

        Assert.Equal("{postgres1.connectionString};Database=imports", db1.Resource.ConnectionStringExpression);
        Assert.Equal("{postgres2.connectionString};Database=imports", db2.Resource.ConnectionStringExpression);
    }
}
