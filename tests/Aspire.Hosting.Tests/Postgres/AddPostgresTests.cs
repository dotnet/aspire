// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json;
using Aspire.Hosting.Postgres;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests.Postgres;

public class AddPostgresTests
{
    [Fact]
    public async Task AddPostgresWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddPostgres("myPostgres");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.GetContainerResources());
        Assert.Equal("myPostgres", containerResource.Name);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(PostgresContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(PostgresContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(PostgresContainerImageTags.Registry, containerAnnotation.Registry);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(5432, endpoint.TargetPort);
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
                Assert.Equal("POSTGRES_USER", env.Key);
                Assert.Equal("postgres", env.Value);
            },
            env =>
            {
                Assert.Equal("POSTGRES_PASSWORD", env.Key);
                Assert.False(string.IsNullOrEmpty(env.Value));
            });
    }

    [Fact]
    public async Task AddPostgresAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.Configuration["Parameters:pass"] = "pass";

        var pass = appBuilder.AddParameter("pass");
        appBuilder.AddPostgres("myPostgres", password: pass, port: 1234);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.GetContainerResources());
        Assert.Equal("myPostgres", containerResource.Name);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(PostgresContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(PostgresContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(PostgresContainerImageTags.Registry, containerAnnotation.Registry);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(5432, endpoint.TargetPort);
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
                Assert.Equal("POSTGRES_USER", env.Key);
                Assert.Equal("postgres", env.Value);
            },
            env =>
            {
                Assert.Equal("POSTGRES_PASSWORD", env.Key);
                Assert.Equal("pass", env.Value);
            });
    }

    [Fact]
    public async Task PostgresCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var postgres = appBuilder.AddPostgres("postgres")
                                 .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000));

        var connectionStringResource = postgres.Resource as IResourceWithConnectionString;

        var connectionString = await connectionStringResource.GetConnectionStringAsync();
        Assert.Equal("Host={postgres.bindings.tcp.host};Port={postgres.bindings.tcp.port};Username=postgres;Password={postgres-password.value}", connectionStringResource.ConnectionStringExpression.ValueExpression);
        Assert.Equal($"Host=localhost;Port=2000;Username=postgres;Password={postgres.Resource.PasswordParameter.Value}", connectionString);
    }

    [Fact]
    public async Task PostgresCreatesConnectionStringWithDatabase()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddPostgres("postgres")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000))
            .AddDatabase("db");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var postgresResource = Assert.Single(appModel.Resources.OfType<PostgresServerResource>());
        var postgresConnectionString = await postgresResource.GetConnectionStringAsync();
        var postgresDatabaseResource = Assert.Single(appModel.Resources.OfType<PostgresDatabaseResource>());
        var postgresDatabaseConnectionStringResource = (IResourceWithConnectionString)postgresDatabaseResource;
        var dbConnectionString = await postgresDatabaseConnectionStringResource.GetConnectionStringAsync();

        Assert.Equal("{postgres.connectionString};Database=db", postgresDatabaseResource.ConnectionStringExpression.ValueExpression);
        Assert.Equal(postgresConnectionString + ";Database=db", dbConnectionString);
    }

    [Fact]
    public async Task AddDatabaseToPostgresAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.Configuration["Parameters:pass"] = "pass";

        var pass = appBuilder.AddParameter("pass");
        appBuilder.AddPostgres("postgres", password: pass, port: 1234).AddDatabase("db");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var containerResources = appModel.GetContainerResources();

        var containerResource = Assert.Single(containerResources);
        Assert.Equal("postgres", containerResource.Name);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(PostgresContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(PostgresContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(PostgresContainerImageTags.Registry, containerAnnotation.Registry);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(5432, endpoint.TargetPort);
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
                Assert.Equal("POSTGRES_USER", env.Key);
                Assert.Equal("postgres", env.Value);
            },
            env =>
            {
                Assert.Equal("POSTGRES_PASSWORD", env.Key);
                Assert.Equal("pass", env.Value);
            });
    }

    [Fact]
    public async Task VerifyManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var pgServer = builder.AddPostgres("pg");
        var db = pgServer.AddDatabase("db");

        var serverManifest = await ManifestUtils.GetManifest(pgServer.Resource);
        var dbManifest = await ManifestUtils.GetManifest(db.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "Host={pg.bindings.tcp.host};Port={pg.bindings.tcp.port};Username=postgres;Password={pg-password.value}",
              "image": "{{PostgresContainerImageTags.Registry}}/{{PostgresContainerImageTags.Image}}:{{PostgresContainerImageTags.Tag}}",
              "env": {
                "POSTGRES_HOST_AUTH_METHOD": "scram-sha-256",
                "POSTGRES_INITDB_ARGS": "--auth-host=scram-sha-256 --auth-local=scram-sha-256",
                "POSTGRES_USER": "postgres",
                "POSTGRES_PASSWORD": "{pg-password.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 5432
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, serverManifest.ToString());

        expectedManifest = """
            {
              "type": "value.v0",
              "connectionString": "{pg.connectionString};Database=db"
            }
            """;
        Assert.Equal(expectedManifest, dbManifest.ToString());
    }

    [Fact]
    public async Task VerifyManifestWithParameters()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var userNameParameter = builder.AddParameter("user");
        var passwordParameter = builder.AddParameter("pass");

        var pgServer = builder.AddPostgres("pg", userNameParameter, passwordParameter);
        var serverManifest = await ManifestUtils.GetManifest(pgServer.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "Host={pg.bindings.tcp.host};Port={pg.bindings.tcp.port};Username={user.value};Password={pass.value}",
              "image": "{{PostgresContainerImageTags.Registry}}/{{PostgresContainerImageTags.Image}}:{{PostgresContainerImageTags.Tag}}",
              "env": {
                "POSTGRES_HOST_AUTH_METHOD": "scram-sha-256",
                "POSTGRES_INITDB_ARGS": "--auth-host=scram-sha-256 --auth-local=scram-sha-256",
                "POSTGRES_USER": "{user.value}",
                "POSTGRES_PASSWORD": "{pass.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 5432
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, serverManifest.ToString());

        pgServer = builder.AddPostgres("pg2", userNameParameter);
        serverManifest = await ManifestUtils.GetManifest(pgServer.Resource);

        expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "Host={pg2.bindings.tcp.host};Port={pg2.bindings.tcp.port};Username={user.value};Password={pg2-password.value}",
              "image": "{{PostgresContainerImageTags.Registry}}/{{PostgresContainerImageTags.Image}}:{{PostgresContainerImageTags.Tag}}",
              "env": {
                "POSTGRES_HOST_AUTH_METHOD": "scram-sha-256",
                "POSTGRES_INITDB_ARGS": "--auth-host=scram-sha-256 --auth-local=scram-sha-256",
                "POSTGRES_USER": "{user.value}",
                "POSTGRES_PASSWORD": "{pg2-password.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 5432
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, serverManifest.ToString());

        pgServer = builder.AddPostgres("pg3", password: passwordParameter);
        serverManifest = await ManifestUtils.GetManifest(pgServer.Resource);

        expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "Host={pg3.bindings.tcp.host};Port={pg3.bindings.tcp.port};Username=postgres;Password={pass.value}",
              "image": "{{PostgresContainerImageTags.Registry}}/{{PostgresContainerImageTags.Image}}:{{PostgresContainerImageTags.Tag}}",
              "env": {
                "POSTGRES_HOST_AUTH_METHOD": "scram-sha-256",
                "POSTGRES_INITDB_ARGS": "--auth-host=scram-sha-256 --auth-local=scram-sha-256",
                "POSTGRES_USER": "postgres",
                "POSTGRES_PASSWORD": "{pass.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 5432
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, serverManifest.ToString());
    }

    [Fact]
    public void WithPgAdminAddsContainer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddPostgres("mypostgres").WithPgAdmin(8081);

        var container = builder.Resources.Single(r => r.Name == "mypostgres-pgadmin");
        var volume = container.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.True(File.Exists(volume.Source)); // File should exist, but will be empty.
        Assert.Equal("/pgadmin4/servers.json", volume.Target);
    }

    [Fact]
    public void WithPostgresTwiceEndsUpWithOneContainer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddPostgres("mypostgres1").WithPgAdmin(8081);
        builder.AddPostgres("mypostgres2").WithPgAdmin(8081);

        builder.Resources.Single(r => r.Name.EndsWith("-pgadmin"));
    }

    [Theory]
    [InlineData("host.docker.internal")]
    [InlineData("host.containers.internal")]
    public void WithPostgresProducesValidServersJsonFile(string containerHost)
    {
        var builder = DistributedApplication.CreateBuilder();
        var pg1 = builder.AddPostgres("mypostgres1").WithPgAdmin(8081);
        var pg2 = builder.AddPostgres("mypostgres2").WithPgAdmin(8081);

        // Add fake allocated endpoints.
        pg1.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001, containerHost));
        pg2.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5002, "host2"));

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
        Assert.Equal(containerHost, servers.GetProperty("1").GetProperty("Host").GetString());
        Assert.Equal(5001, servers.GetProperty("1").GetProperty("Port").GetInt32());
        Assert.Equal("postgres", servers.GetProperty("1").GetProperty("Username").GetString());
        Assert.Equal("prefer", servers.GetProperty("1").GetProperty("SSLMode").GetString());
        Assert.Equal("postgres", servers.GetProperty("1").GetProperty("MaintenanceDB").GetString());
        Assert.Equal($"echo '{pg1.Resource.PasswordParameter.Value}'", servers.GetProperty("1").GetProperty("PasswordExecCommand").GetString());

        // Make sure the second server is correct.
        Assert.Equal(pg2.Resource.Name, servers.GetProperty("2").GetProperty("Name").GetString());
        Assert.Equal("Aspire instances", servers.GetProperty("2").GetProperty("Group").GetString());
        Assert.Equal("host2", servers.GetProperty("2").GetProperty("Host").GetString());
        Assert.Equal(5002, servers.GetProperty("2").GetProperty("Port").GetInt32());
        Assert.Equal("postgres", servers.GetProperty("2").GetProperty("Username").GetString());
        Assert.Equal("prefer", servers.GetProperty("2").GetProperty("SSLMode").GetString());
        Assert.Equal("postgres", servers.GetProperty("2").GetProperty("MaintenanceDB").GetString());
        Assert.Equal($"echo '{pg2.Resource.PasswordParameter.Value}'", servers.GetProperty("2").GetProperty("PasswordExecCommand").GetString());
    }

    [Fact]
    public void ThrowsWithIdenticalChildResourceNames()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var db = builder.AddPostgres("postgres1");
        db.AddDatabase("db");

        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [Fact]
    public void ThrowsWithIdenticalChildResourceNamesDifferentParents()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.AddPostgres("postgres1")
            .AddDatabase("db");

        var db = builder.AddPostgres("postgres2");
        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [Fact]
    public void CanAddDatabasesWithDifferentNamesOnSingleServer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var postgres1 = builder.AddPostgres("postgres1");

        var db1 = postgres1.AddDatabase("db1", "customers1");
        var db2 = postgres1.AddDatabase("db2", "customers2");

        Assert.Equal("customers1", db1.Resource.DatabaseName);
        Assert.Equal("customers2", db2.Resource.DatabaseName);

        Assert.Equal("{postgres1.connectionString};Database=customers1", db1.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal("{postgres1.connectionString};Database=customers2", db2.Resource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public void CanAddDatabasesWithTheSameNameOnMultipleServers()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var db1 = builder.AddPostgres("postgres1")
            .AddDatabase("db1", "imports");

        var db2 = builder.AddPostgres("postgres2")
            .AddDatabase("db2", "imports");

        Assert.Equal("imports", db1.Resource.DatabaseName);
        Assert.Equal("imports", db2.Resource.DatabaseName);

        Assert.Equal("{postgres1.connectionString};Database=imports", db1.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal("{postgres2.connectionString};Database=imports", db2.Resource.ConnectionStringExpression.ValueExpression);
    }
}
