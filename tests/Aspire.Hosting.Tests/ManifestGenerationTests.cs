// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Components.Common.TestUtilities;
using Aspire.Hosting.Postgres;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Redis;
using Aspire.Hosting.Tests.Helpers;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class ManifestGenerationTests
{
    [Fact]
    public void EnsureAddParameterWithSecretFalseDoesntEmitSecretField()
    {
        using var program = CreateTestProgramJsonDocumentManifestPublisher();
        program.AppBuilder.AddParameter("x", secret: false);
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");
        var x = resources.GetProperty("x");
        var inputs = x.GetProperty("inputs");
        var value = inputs.GetProperty("value");
        Assert.False(value.TryGetProperty("secret", out _));
    }

    [Fact]
    public void EnsureAddParameterWithSecretDefaultDoesntEmitSecretField()
    {
        using var program = CreateTestProgramJsonDocumentManifestPublisher();
        program.AppBuilder.AddParameter("x");
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");
        var x = resources.GetProperty("x");
        var inputs = x.GetProperty("inputs");
        var value = inputs.GetProperty("value");
        Assert.False(value.TryGetProperty("secret", out _));
    }

    [Fact]
    public void EnsureAddParameterWithSecretTrueDoesEmitSecretField()
    {
        using var program = CreateTestProgramJsonDocumentManifestPublisher();
        program.AppBuilder.AddParameter("x", secret: true);
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");
        var x = resources.GetProperty("x");
        var inputs = x.GetProperty("inputs");
        var value = inputs.GetProperty("value");
        Assert.True(value.TryGetProperty("secret", out var secret));
        Assert.True(secret.GetBoolean());
    }

    [Fact]
    public void EnsureWorkerProjectDoesNotGetBindingsGenerated()
    {
        using var program = CreateTestProgramJsonDocumentManifestPublisher();

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var workerA = resources.GetProperty("workera");
        Assert.False(workerA.TryGetProperty("bindings", out _));
    }

    [Fact]
    public async Task WithContainerRegistryUpdatesContainerImageAnnotationsDuringPublish()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = GetManifestArgs(),
            ContainerRegistryOverride = "myprivateregistry.company.com"
        });

        var redis = builder.AddContainer("redis", "redis");
        builder.Build().Run();

        var redisManifest = await ManifestUtils.GetManifest(redis.Resource).DefaultTimeout();
        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "image": "myprivateregistry.company.com/redis:latest"
            }
            """;
        Assert.Equal(expectedManifest, redisManifest.ToString());
    }

    [Fact]
    public void ExcludeLaunchProfileOmitsBindings()
    {
        var appBuilder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        { Args = GetManifestArgs(), DisableDashboard = true, AssemblyName = typeof(ManifestGenerationTests).Assembly.FullName });

        appBuilder.AddProject<Projects.ServiceA>("servicea", launchProfileName: null);

        appBuilder.Services.AddKeyedSingleton<IDistributedApplicationPublisher, JsonDocumentManifestPublisher>("manifest");

        using var program = appBuilder.Build();
        var publisher = program.Services.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        Assert.False(
            resources.GetProperty("servicea").TryGetProperty("bindings", out _),
            "Service has no bindings because they weren't populated from the launch profile.");
    }

    [Theory]
    [InlineData(new string[] { "args1", "args2" }, new string[] { "withArgs1", "withArgs2" })]
    [InlineData(new string[] { }, new string[] { "withArgs1", "withArgs2" })]
    [InlineData(new string[] { "args1", "args2" }, new string[] { })]
    public void EnsureExecutableWithArgsEmitsExecutableArgs(string[] addExecutableArgs, string[] withArgsArgs)
    {
        using var program = CreateTestProgramJsonDocumentManifestPublisher();

        var resourceBuilder = program.AppBuilder.AddExecutable("program", "run program", "c:/", addExecutableArgs);
        if (withArgsArgs.Length > 0)
        {
            resourceBuilder.WithArgs(withArgsArgs);
        }

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var resource = resources.GetProperty("program");
        var args = resource.GetProperty("args");
        Assert.Equal(addExecutableArgs.Length + withArgsArgs.Length, args.GetArrayLength());

        var verify = new List<Action<JsonElement>>();
        foreach (var addExecutableArg in addExecutableArgs)
        {
            verify.Add(arg => Assert.Equal(addExecutableArg, arg.GetString()));
        }

        foreach (var withArgsArg in withArgsArgs)
        {
            verify.Add(arg => Assert.Equal(withArgsArg, arg.GetString()));
        }

        Assert.Collection(args.EnumerateArray(), [.. verify]);
    }

    [Fact]
    public void ExecutableManifestNotIncludeArgsWhenEmpty()
    {
        using var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddExecutable("program", "run program", "c:/");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var resource = resources.GetProperty("program");
        var exists = resource.TryGetProperty("args", out _);
        Assert.False(exists);
    }

    [Fact]
    public void EnsureAllRedisManifestTypesHaveVersion0Suffix()
    {
        using var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddRedis("rediscontainer");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var container = resources.GetProperty("rediscontainer");
        Assert.Equal("container.v0", container.GetProperty("type").GetString());
    }

    [Fact]
    public void PublishingRedisResourceAsContainerResultsInConnectionStringProperty()
    {
        using var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddRedis("rediscontainer");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var container = resources.GetProperty("rediscontainer");
        Assert.Equal("container.v0", container.GetProperty("type").GetString());
        Assert.Equal("{rediscontainer.bindings.tcp.host}:{rediscontainer.bindings.tcp.port},password={rediscontainer-password.value}", container.GetProperty("connectionString").GetString());
    }

    [Fact]
    public void EnsureAllPostgresManifestTypesHaveVersion0Suffix()
    {
        using var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddPostgres("postgrescontainer").AddDatabase("postgresdatabase");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var server = resources.GetProperty("postgrescontainer");
        Assert.Equal("container.v0", server.GetProperty("type").GetString());

        var db = resources.GetProperty("postgresdatabase");
        Assert.Equal("value.v0", db.GetProperty("type").GetString());
    }

    [Fact]
    public void MetadataPropertyNotEmittedWhenMetadataNotAdded()
    {
        using var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddContainer("testresource", "testresource");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var container = resources.GetProperty("testresource");
        Assert.False(container.TryGetProperty("metadata", out var _));
    }

    [Fact]
    public void VerifyTestProgramFullManifest()
    {
        using var program = CreateTestProgramJsonDocumentManifestPublisher(includeIntegrationServices: true);

        program.AppBuilder.Services.Configure<PublishingOptions>(options =>
        {
            // set the output path so the paths are relative to the AppHostDirectory
            options.OutputPath = program.AppBuilder.AppHostDirectory;
        });

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var expectedManifest = $$"""
            {
              "$schema": "{{SchemaUtils.SchemaVersion}}",
              "resources": {
                "servicea": {
                  "type": "project.v0",
                  "path": "testproject/TestProject.ServiceA/TestProject.ServiceA.csproj",
                  "env": {
                    "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES": "true",
                    "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES": "true",
                    "OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY": "in_memory",
                    "ASPNETCORE_FORWARDEDHEADERS_ENABLED": "true",
                    "HTTP_PORTS": "{servicea.bindings.http.targetPort}"
                  },
                  "bindings": {
                    "http": {
                      "scheme": "http",
                      "protocol": "tcp",
                      "transport": "http"
                    },
                    "https": {
                      "scheme": "https",
                      "protocol": "tcp",
                      "transport": "http"
                    }
                  }
                },
                "serviceb": {
                  "type": "project.v0",
                  "path": "testproject/TestProject.ServiceB/TestProject.ServiceB.csproj",
                  "env": {
                    "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES": "true",
                    "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES": "true",
                    "OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY": "in_memory",
                    "ASPNETCORE_FORWARDEDHEADERS_ENABLED": "true",
                    "HTTP_PORTS": "{serviceb.bindings.http.targetPort}"
                  },
                  "bindings": {
                    "http": {
                      "scheme": "http",
                      "protocol": "tcp",
                      "transport": "http"
                    },
                    "https": {
                      "scheme": "https",
                      "protocol": "tcp",
                      "transport": "http"
                    }
                  }
                },
                "servicec": {
                  "type": "project.v0",
                  "path": "testproject/TestProject.ServiceC/TestProject.ServiceC.csproj",
                  "env": {
                    "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES": "true",
                    "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES": "true",
                    "OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY": "in_memory",
                    "ASPNETCORE_FORWARDEDHEADERS_ENABLED": "true",
                    "Kestrel__Endpoints__http__Url": "http://*:{servicec.bindings.http.targetPort}"
                  },
                  "bindings": {
                    "http": {
                      "scheme": "http",
                      "protocol": "tcp",
                      "transport": "http",
                      "targetPort": 5271
                    },
                    "https": {
                      "scheme": "https",
                      "protocol": "tcp",
                      "transport": "http",
                      "targetPort": 5271
                    }
                  }
                },
                "workera": {
                  "type": "project.v0",
                  "path": "testproject/TestProject.WorkerA/TestProject.WorkerA.csproj",
                  "env": {
                    "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES": "true",
                    "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES": "true",
                    "OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY": "in_memory"
                  }
                },
                "integrationservicea": {
                  "type": "project.v0",
                  "path": "testproject/TestProject.IntegrationServiceA/TestProject.IntegrationServiceA.csproj",
                  "env": {
                    "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES": "true",
                    "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES": "true",
                    "OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY": "in_memory",
                    "ASPNETCORE_FORWARDEDHEADERS_ENABLED": "true",
                    "HTTP_PORTS": "{integrationservicea.bindings.http.targetPort}",
                    "SKIP_RESOURCES": "None",
                    "ConnectionStrings__redis": "{redis.connectionString}",
                    "ConnectionStrings__postgresdb": "{postgresdb.connectionString}"
                  },
                  "bindings": {
                    "http": {
                      "scheme": "http",
                      "protocol": "tcp",
                      "transport": "http"
                    },
                    "https": {
                      "scheme": "https",
                      "protocol": "tcp",
                      "transport": "http"
                    }
                  }
                },
                "redis": {
                  "type": "container.v0",
                  "connectionString": "{redis.bindings.tcp.host}:{redis.bindings.tcp.port},password={redis-password.value}",
                  "image": "{{ComponentTestConstants.AspireTestContainerRegistry}}/{{RedisContainerImageTags.Image}}:{{RedisContainerImageTags.Tag}}",
                  "entrypoint": "/bin/sh",
                  "args": [
                    "-c",
                    "redis-server --requirepass $REDIS_PASSWORD"
                  ],
                  "env": {
                    "REDIS_PASSWORD": "{redis-password.value}"
                  },
                  "bindings": {
                    "tcp": {
                      "scheme": "tcp",
                      "protocol": "tcp",
                      "transport": "tcp",
                      "targetPort": 6379
                    }
                  }
                },
                "postgres": {
                  "type": "container.v0",
                  "connectionString": "Host={postgres.bindings.tcp.host};Port={postgres.bindings.tcp.port};Username=postgres;Password={postgres-password.value}",
                  "image": "{{ComponentTestConstants.AspireTestContainerRegistry}}/{{PostgresContainerImageTags.Image}}:{{PostgresContainerImageTags.Tag}}",
                  "env": {
                    "POSTGRES_HOST_AUTH_METHOD": "scram-sha-256",
                    "POSTGRES_INITDB_ARGS": "--auth-host=scram-sha-256 --auth-local=scram-sha-256",
                    "POSTGRES_USER": "postgres",
                    "POSTGRES_PASSWORD": "{postgres-password.value}",
                    "POSTGRES_DB": "postgresdb"
                  },
                  "bindings": {
                    "tcp": {
                      "scheme": "tcp",
                      "protocol": "tcp",
                      "transport": "tcp",
                      "targetPort": 5432
                    }
                  }
                },
                "postgresdb": {
                  "type": "value.v0",
                  "connectionString": "{postgres.connectionString};Database=postgresdb"
                },
                "redis-password": {
                  "type": "parameter.v0",
                  "value": "{redis-password.inputs.value}",
                  "inputs": {
                    "value": {
                      "type": "string",
                      "secret": true,
                      "default": {
                        "generate": {
                          "minLength": 22,
                          "special": false
                        }
                      }
                    }
                  }
                },
                "postgres-password": {
                  "type": "parameter.v0",
                  "value": "{postgres-password.inputs.value}",
                  "inputs": {
                    "value": {
                      "type": "string",
                      "secret": true,
                      "default": {
                        "generate": {
                          "minLength": 22
                        }
                      }
                    }
                  }
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, publisher.ManifestDocument.RootElement.ToString());
    }

    [Fact]
    public async Task ParameterInputDefaultValuesGenerateCorrectly()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var param = appBuilder.AddParameter("param");
        param.Resource.Default = new GenerateParameterDefault()
        {
            MinLength = 16,
            Lower = false,
            Upper = false,
            Numeric = false,
            Special = false,
            MinLower = 1,
            MinUpper = 2,
            MinNumeric = 3,
            MinSpecial = 4,
        };

        var expectedManifest = """
            {
              "type": "parameter.v0",
              "value": "{param.inputs.value}",
              "inputs": {
                "value": {
                  "type": "string",
                  "default": {
                    "generate": {
                      "minLength": 16,
                      "lower": false,
                      "upper": false,
                      "numeric": false,
                      "special": false,
                      "minLower": 1,
                      "minUpper": 2,
                      "minNumeric": 3,
                      "minSpecial": 4
                    }
                  }
                }
              }
            }
            """;

        var manifest = await ManifestUtils.GetManifest(param.Resource).DefaultTimeout();
        Assert.Equal(expectedManifest, manifest.ToString());
    }

    private static TestProgram CreateTestProgramJsonDocumentManifestPublisher(bool includeIntegrationServices = false, bool includeNodeApp = false)
    {
        var program = TestProgram.Create<ManifestGenerationTests>(GetManifestArgs(), includeIntegrationServices, includeNodeApp);
        program.AppBuilder.Services.AddKeyedSingleton<IDistributedApplicationPublisher, JsonDocumentManifestPublisher>("manifest");
        return program;
    }

    private static string[] GetManifestArgs()
    {
        var manifestPath = Path.Combine(Path.GetTempPath(), "tempmanifests", Guid.NewGuid().ToString(), "manifest.json");
        return ["--publisher", "manifest", "--output-path", manifestPath];
    }
}
