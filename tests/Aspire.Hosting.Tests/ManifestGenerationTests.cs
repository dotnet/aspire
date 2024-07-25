// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Components.Common.Tests;
using Aspire.Hosting.Garnet;
using Aspire.Hosting.Postgres;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.RabbitMQ;
using Aspire.Hosting.Redis;
using Aspire.Hosting.Tests.Helpers;
using Aspire.Hosting.Utils;
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

        var redisManifest = await ManifestUtils.GetManifest(redis.Resource);
        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "image": "myprivateregistry.company.com/redis:latest"
            }
            """;
        Assert.Equal(expectedManifest, redisManifest.ToString());
    }

    [Fact]
    public void EnsureExecutablesWithDockerfileProduceDockerfilev0Manifest()
    {
        using var program = CreateTestProgramJsonDocumentManifestPublisher(includeNodeApp: true);
        program.NodeAppBuilder!.WithHttpsEndpoint(targetPort: 3000, env: "HTTPS_PORT")
            .PublishAsDockerFile();

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        // NPM app should still be executable.v0
        var npmapp = resources.GetProperty("npmapp");
        Assert.Equal("executable.v0", npmapp.GetProperty("type").GetString());
        Assert.DoesNotContain("\\", npmapp.GetProperty("workingDirectory").GetString());

        // Node app should now be dockerfile.v0
        var nodeapp = resources.GetProperty("nodeapp");
        Assert.Equal("dockerfile.v0", nodeapp.GetProperty("type").GetString());
        Assert.True(nodeapp.TryGetProperty("path", out _));
        Assert.True(nodeapp.TryGetProperty("context", out _));
        Assert.True(nodeapp.TryGetProperty("env", out var env));
        Assert.True(nodeapp.TryGetProperty("bindings", out var bindings));

        Assert.Equal(3000, bindings.GetProperty("https").GetProperty("targetPort").GetInt32());
        Assert.Equal("https", bindings.GetProperty("https").GetProperty("scheme").GetString());
        Assert.Equal("{nodeapp.bindings.https.targetPort}", env.GetProperty("HTTPS_PORT").GetString());
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
        Assert.Equal("{rediscontainer.bindings.tcp.host}:{rediscontainer.bindings.tcp.port}", container.GetProperty("connectionString").GetString());
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
    public void EnsureAllRabbitMQManifestTypesHaveVersion0Suffix()
    {
        using var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddRabbitMQ("rabbitcontainer");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var server = resources.GetProperty("rabbitcontainer");
        Assert.Equal("container.v0", server.GetProperty("type").GetString());
    }

    [Fact]
    public void NodeAppIsExecutableResource()
    {
        using var program = CreateTestProgramJsonDocumentManifestPublisher();

        program.AppBuilder.AddNodeApp("nodeapp", "..\\foo\\app.js")
            .WithHttpEndpoint(port: 5031, env: "PORT");
        program.AppBuilder.AddNpmApp("npmapp", "..\\foo")
            .WithHttpEndpoint(port: 5032, env: "PORT");

        // Build AppHost so that publisher can be resolved.
        program.Build();
        var publisher = program.GetManifestPublisher();

        program.Run();

        var resources = publisher.ManifestDocument.RootElement.GetProperty("resources");

        var nodeApp = resources.GetProperty("nodeapp");
        var npmApp = resources.GetProperty("npmapp");

        static void AssertNodeResource(TestProgram program, string resourceName, JsonElement jsonElement, string expectedCommand, string[] expectedArgs)
        {
            var s = jsonElement.ToString();
            Assert.Equal("executable.v0", jsonElement.GetProperty("type").GetString());

            var bindings = jsonElement.GetProperty("bindings");
            var httpBinding = bindings.GetProperty("http");

            Assert.Equal("http", httpBinding.GetProperty("scheme").GetString());

            var env = jsonElement.GetProperty("env");
            Assert.Equal($$"""{{{resourceName}}.bindings.http.targetPort}""", env.GetProperty("PORT").GetString());
            Assert.Equal(program.AppBuilder.Environment.EnvironmentName.ToLowerInvariant(), env.GetProperty("NODE_ENV").GetString());

            var command = jsonElement.GetProperty("command");
            Assert.Equal(expectedCommand, command.GetString());
            Assert.Equal(expectedArgs, jsonElement.GetProperty("args").EnumerateArray().Select(e => e.GetString()).ToArray());
        }

        AssertNodeResource(program, "nodeapp", nodeApp, "node", ["..\\foo\\app.js"]);
        AssertNodeResource(program, "npmapp", npmApp, "npm", ["run", "start"]);
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
                    "ConnectionStrings__tempdb": "{tempdb.connectionString}",
                    "ConnectionStrings__redis": "{redis.connectionString}",
                    "ConnectionStrings__garnet": "{garnet.connectionString}",
                    "ConnectionStrings__postgresdb": "{postgresdb.connectionString}",
                    "ConnectionStrings__rabbitmq": "{rabbitmq.connectionString}",
                    "ConnectionStrings__freepdb1": "{freepdb1.connectionString}",
                    "ConnectionStrings__cosmos": "{cosmos.connectionString}",
                    "ConnectionStrings__eventhubns": "{eventhubns.connectionString}"
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
                "sqlserver": {
                  "type": "container.v0",
                  "connectionString": "Server={sqlserver.bindings.tcp.host},{sqlserver.bindings.tcp.port};User ID=sa;Password={sqlserver-password.value};TrustServerCertificate=true",
                  "image": "{{SqlServerContainerImageTags.Registry}}/{{SqlServerContainerImageTags.Image}}:{{SqlServerContainerImageTags.Tag}}",
                  "env": {
                    "ACCEPT_EULA": "Y",
                    "MSSQL_SA_PASSWORD": "{sqlserver-password.value}"
                  },
                  "bindings": {
                    "tcp": {
                      "scheme": "tcp",
                      "protocol": "tcp",
                      "transport": "tcp",
                      "targetPort": 1433
                    }
                  }
                },
                "tempdb": {
                  "type": "value.v0",
                  "connectionString": "{sqlserver.connectionString};Database=tempdb"
                },
                "redis": {
                  "type": "container.v0",
                  "connectionString": "{redis.bindings.tcp.host}:{redis.bindings.tcp.port}",
                  "image": "{{TestConstants.AspireTestContainerRegistry}}/{{RedisContainerImageTags.Image}}:{{RedisContainerImageTags.Tag}}",
                  "bindings": {
                    "tcp": {
                      "scheme": "tcp",
                      "protocol": "tcp",
                      "transport": "tcp",
                      "targetPort": 6379
                    }
                  }
                },
                "garnet": {
                  "type": "container.v0",
                  "connectionString": "{garnet.bindings.tcp.host}:{garnet.bindings.tcp.port}",
                  "image": "{{GarnetContainerImageTags.Registry}}/{{GarnetContainerImageTags.Image}}:{{GarnetContainerImageTags.Tag}}",
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
                  "image": "{{TestConstants.AspireTestContainerRegistry}}/{{PostgresContainerImageTags.Image}}:{{PostgresContainerImageTags.Tag}}",
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
                "rabbitmq": {
                  "type": "container.v0",
                  "connectionString": "amqp://guest:{rabbitmq-password.value}@{rabbitmq.bindings.tcp.host}:{rabbitmq.bindings.tcp.port}",
                  "image": "{{TestConstants.AspireTestContainerRegistry}}/{{RabbitMQContainerImageTags.Image}}:{{RabbitMQContainerImageTags.Tag}}",
                  "env": {
                    "RABBITMQ_DEFAULT_USER": "guest",
                    "RABBITMQ_DEFAULT_PASS": "{rabbitmq-password.value}"
                  },
                  "bindings": {
                    "tcp": {
                      "scheme": "tcp",
                      "protocol": "tcp",
                      "transport": "tcp",
                      "targetPort": 5672
                    }
                  }
                },
                "oracledatabase": {
                  "type": "container.v0",
                  "connectionString": "user id=system;password={oracledatabase-password.value};data source={oracledatabase.bindings.tcp.host}:{oracledatabase.bindings.tcp.port}",
                  "image": "{{OracleContainerImageTags.Registry}}/{{OracleContainerImageTags.Image}}:{{OracleContainerImageTags.Tag}}",
                  "env": {
                    "ORACLE_PWD": "{oracledatabase-password.value}"
                  },
                  "bindings": {
                    "tcp": {
                      "scheme": "tcp",
                      "protocol": "tcp",
                      "transport": "tcp",
                      "targetPort": 1521
                    }
                  }
                },
                "freepdb1": {
                  "type": "value.v0",
                  "connectionString": "{oracledatabase.connectionString}/freepdb1"
                },
                "cosmos": {
                  "type": "azure.bicep.v0",
                  "connectionString": "{cosmos.secretOutputs.connectionString}",
                  "path": "cosmos.module.bicep",
                  "params": {
                    "keyVaultName": ""
                  }
                },
                "eventhubns": {
                  "type": "azure.bicep.v0",
                  "connectionString": "{eventhubns.outputs.eventHubsEndpoint}",
                  "path": "eventhubns.module.bicep",
                  "params": {
                    "principalId": "",
                    "principalType": ""
                  }
                },
                "sqlserver-password": {
                  "type": "parameter.v0",
                  "value": "{sqlserver-password.inputs.value}",
                  "inputs": {
                    "value": {
                      "type": "string",
                      "secret": true,
                      "default": {
                        "generate": {
                          "minLength": 22,
                          "minLower": 1,
                          "minUpper": 1,
                          "minNumeric": 1
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
                },
                "rabbitmq-password": {
                  "type": "parameter.v0",
                  "value": "{rabbitmq-password.inputs.value}",
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
                "oracledatabase-password": {
                  "type": "parameter.v0",
                  "value": "{oracledatabase-password.inputs.value}",
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

        var manifest = await ManifestUtils.GetManifest(param.Resource);
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
        var manifestPath = Path.GetTempFileName();
        return ["--publisher", "manifest", "--output-path", manifestPath];
    }
}
