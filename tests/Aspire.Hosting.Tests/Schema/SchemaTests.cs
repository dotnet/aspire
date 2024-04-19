// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;
using Aspire.Hosting.Tests.Helpers;
using Aspire.Hosting.Utils;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Linq;
using Amazon;

namespace Aspire.Hosting.Tests.Schema;

public class SchemaTests
{
    public static TheoryData<string, Action<IDistributedApplicationBuilder>> ApplicationSamples
    {
        get
        {
            var data = new TheoryData<string, Action<IDistributedApplicationBuilder>>
            {
                { "BasicParameter", (IDistributedApplicationBuilder builder) =>
                    {
                        builder.AddParameter("foo");
                    }
                },

                { "BasicSecretParameter", (IDistributedApplicationBuilder builder) =>
                    {
                        builder.AddParameter("foo", secret: true);
                    }
                },

                { "ConnectionStringParameter", (IDistributedApplicationBuilder builder) =>
                    {
                        builder.AddConnectionString("foo");
                    }
                },

                { "BasicContainer", (IDistributedApplicationBuilder builder) =>
                    {
                        builder.AddRedis("redis");
                    }
                },

                { "ContainerWithVolume", (IDistributedApplicationBuilder builder) =>
                    {
                        builder.AddRedis("redis").WithDataVolume();
                    }
                },

                { "ContainerWithBindMount", (IDistributedApplicationBuilder builder) =>
                    {
                        builder.AddRedis("redis").WithBindMount("source", "target");
                    }
                },

                { "BasicContainerWithConnectionString", (IDistributedApplicationBuilder builder) =>
                    {
                        builder.AddPostgres("postgres");
                    }
                },

                { "CdkResourceWithChildResource", (IDistributedApplicationBuilder builder) =>
                    {
                        builder.AddPostgres("postgres").PublishAsAzurePostgresFlexibleServer().AddDatabase("db");
                    }
                },

                { "BasicDockerfile", (IDistributedApplicationBuilder builder) =>
                    {
                        builder.AddExecutable("foo", "bar", "baz", "one", "two", "three").PublishAsDockerFile();
                    }
                },

                { "ContainerWithContainerRunArgs", (IDistributedApplicationBuilder builder) =>
                    {
                        builder.AddContainer("foo", "bar").WithContainerRunArgs("one", "two", "three");
                    }
                },

                { "BasicProject", (IDistributedApplicationBuilder builder) =>
                    {
                        builder.AddProject<Projects.ServiceA>("project");
                    }
                },

                { "BasicExecutable", (IDistributedApplicationBuilder builder) =>
                    {
                        builder.AddExecutable("executable", "hellworld", "foo", "arg1", "arg2");
                    }
                },

                { "AwsStack", (IDistributedApplicationBuilder builder) =>
                    {
                        var awsSdkConfig = builder.AddAWSSDKConfig()
                                                  .WithRegion(RegionEndpoint.USWest2)
                                                  .WithProfile("test-profile");

                        builder.AddAWSCloudFormationStack("ExistingStack")
                               .WithReference(awsSdkConfig);
                    }
                },

                { "AwsTemplate", (IDistributedApplicationBuilder builder) =>
                    {
                        var awsSdkConfig = builder.AddAWSSDKConfig()
                                                  .WithRegion(RegionEndpoint.USWest2)
                                                  .WithProfile("test-profile");

                        builder.AddAWSCloudFormationTemplate("TemplateStack", "nonexistenttemplate")
                               .WithReference(awsSdkConfig);
                    }
                },

                { "DaprWithComponents", (IDistributedApplicationBuilder builder) =>
                    {
                        var dapr = builder.AddDapr(dopts =>
                        {
                            // Just to avoid dynamic discovery which will throw.
                            dopts.DaprPath = "notrealpath";
                        });
                        var state = dapr.AddDaprStateStore("daprstate");
                        var pubsub = dapr.AddDaprPubSub("daprpubsub");

                        builder.AddProject<Projects.ServiceA>("project")
                               .WithDaprSidecar()
                               .WithReference(state)
                               .WithReference(pubsub);
                    }
                }
            };

            return data;
        }
    }

    private static JSchema? s_schema;

    private static async Task<JSchema> GetSchemaAsync()
    {
        if (s_schema == null)
        {
            var relativePath = Path.Combine("Schema", "aspire-8.0.json");
            var schemaPath = Path.GetFullPath(relativePath);
            var schemaText = await File.ReadAllTextAsync(schemaPath);
            s_schema = JSchema.Parse(schemaText);
        }

        return s_schema;
    }

    [SkipOnHelixTheory]
    [MemberData(nameof(ApplicationSamples))]
    public async Task ValidateApplicationSamples(string testCaseName, Action<IDistributedApplicationBuilder> configurator)
    {
        _ = testCaseName;

        var builder = TestDistributedApplicationBuilder.Create(new DistributedApplicationOptions
        {
            Args = ["--publisher", "manifest", "--output-path", "not-used.json"]
        });
        builder.Services.AddKeyedSingleton<IDistributedApplicationPublisher, JsonDocumentManifestPublisher>("manifest");
        configurator(builder);

        using var program = builder.Build();
        var publisher = program.Services.GetManifestPublisher();

        program.Run();

        var manifestText = publisher.ManifestDocument.RootElement.ToString();
        var manifestJson = JToken.Parse(manifestText);
        var schema = await GetSchemaAsync();
        var isValid = manifestJson.IsValid(schema, out IList<ValidationError> errors);

        if (!isValid)
        {
            var errorMessages = errors.Select(e => e.Message).ToList();
            Assert.True(isValid, string.Join(Environment.NewLine, errorMessages));
        }
    }

    [Fact]
    public async Task SchemaRejectsEmptyManifest()
    {
        var manifestTest = """
            {
            }
            """;

        var manifestJson = JToken.Parse(manifestTest);
        var schema = await GetSchemaAsync();
        Assert.False(manifestJson.IsValid(schema));
    }

    [SkipOnHelixFact]
    public async Task ManifestAcceptsUnknownResourceType()
    {
        var manifestTest = """
            {
              "resources": {
                "aresource": {
                  "type": "not.a.valid.resource"
                }
              }
            }
            """;

        var manifestJson = JToken.Parse(manifestTest);
        var schema = await GetSchemaAsync();
        Assert.True(manifestJson.IsValid(schema));
    }

    [SkipOnHelixFact]
    public async Task ManifestWithContainerResourceWithMissingImageIsRejected()
    {
        var manifestTest = """
            {
              "resources": {
                "mycontainer": {
                  "type": "container.v0"
                }
              }
            }
            """;

        var manifestJson = JToken.Parse(manifestTest);
        var schema = await GetSchemaAsync();
        Assert.False(manifestJson.IsValid(schema));
    }

    [SkipOnHelixFact]
    public async Task ManifestWithValue0ResourceWithConnectionStringAndValueIsRejectedIsRejected()
    {
        var manifestTest = """
            {
              "resources": {
                "valueresource": {
                  "type": "value.v0",
                  "connectionString": "{valueresource.value}",
                  "value": "this.should.not.be.here"
                }
              }
            }
            """;

        var manifestJson = JToken.Parse(manifestTest);
        var schema = await GetSchemaAsync();
        Assert.False(manifestJson.IsValid(schema));
    }

    [SkipOnHelixFact]
    public async Task ManifestWithContainerResourceAndNoEnvOrBindingsIsAccepted()
    {
        var manifestTest = """
            {
              "resources": {
                "mycontainer": {
                  "type": "container.v0",
                  "image": "myimage:latest"
                }
              }
            }
            """;

        var manifestJson = JToken.Parse(manifestTest);
        var schema = await GetSchemaAsync();
        Assert.True(manifestJson.IsValid(schema));
    }

    [SkipOnHelixFact]
    public async Task ManifestWithProjectResourceAndNoEnvOrBindingsIsAccepted()
    {
        var manifestTest = """
            {
              "resources": {
                "myapp": {
                  "type": "project.v0",
                  "path": "path/to/project/project.csproj"
                }
              }
            }
            """;

        var manifestJson = JToken.Parse(manifestTest);
        var schema = await GetSchemaAsync();
        var isValid = manifestJson.IsValid(schema, out IList<ValidationError> errors);

        if (!isValid)
        {
            var errorMessages = errors.Select(e => e.Message).ToList();
            Assert.True(isValid, string.Join(Environment.NewLine, errorMessages));
        }
    }

    [SkipOnHelixFact]
    public async Task BicepManifestIsAccepted()
    {
        // The reason this large test is here is that when submitting the positive test cases to SchemaStore.org
        // I found this one was failing. So I'm including it here.
        var manifestTest = """
            {
              "resources": {
                "administratorLogin": {
                  "inputs": {
                    "value": {
                      "type": "string"
                    }
                  },
                  "type": "parameter.v0",
                  "value": "{administratorLogin.inputs.value}"
                },
                "administratorLoginPassword": {
                  "inputs": {
                    "value": {
                      "secret": true,
                      "type": "string"
                    }
                  },
                  "type": "parameter.v0",
                  "value": "{administratorLoginPassword.inputs.value}"
                },
                "ai": {
                  "connectionString": "{ai.outputs.appInsightsConnectionString}",
                  "params": {
                    "logAnalyticsWorkspaceId": "{lawkspc.outputs.logAnalyticsWorkspaceId}"
                  },
                  "path": "ai.module.bicep",
                  "type": "azure.bicep.v0"
                },
                "aiwithoutlaw": {
                  "connectionString": "{aiwithoutlaw.outputs.appInsightsConnectionString}",
                  "params": {
                    "logAnalyticsWorkspaceId": ""
                  },
                  "path": "aiwithoutlaw.module.bicep",
                  "type": "azure.bicep.v0"
                },
                "api": {
                  "bindings": {
                    "http": {
                      "external": true,
                      "protocol": "tcp",
                      "scheme": "http",
                      "transport": "http"
                    },
                    "https": {
                      "external": true,
                      "protocol": "tcp",
                      "scheme": "https",
                      "transport": "http"
                    }
                  },
                  "env": {
                    "APPLICATIONINSIGHTS_CONNECTION_STRING": "{ai.connectionString}",
                    "ASPNETCORE_FORWARDEDHEADERS_ENABLED": "true",
                    "ConnectionStrings__appConfig": "{appConfig.connectionString}",
                    "ConnectionStrings__blob": "{blob.connectionString}",
                    "ConnectionStrings__cosmos": "{cosmos.connectionString}",
                    "ConnectionStrings__db": "{db.connectionString}",
                    "ConnectionStrings__db2": "{db2.connectionString}",
                    "ConnectionStrings__kv3": "{kv3.connectionString}",
                    "ConnectionStrings__queue": "{queue.connectionString}",
                    "ConnectionStrings__redis": "{redis.connectionString}",
                    "ConnectionStrings__sb": "{sb.connectionString}",
                    "ConnectionStrings__signalr": "{signalr.connectionString}",
                    "ConnectionStrings__table": "{table.connectionString}",
                    "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES": "true",
                    "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES": "true",
                    "OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY": "in_memory",
                    "bicepValue0": "{test.outputs.val0}",
                    "bicepValue1": "{test.outputs.val1}",
                    "bicepValue_test": "{test.outputs.test}"
                  },
                  "path": "../BicepSample.ApiService/BicepSample.ApiService.csproj",
                  "type": "project.v0"
                },
                "appConfig": {
                  "connectionString": "{appConfig.outputs.appConfigEndpoint}",
                  "params": {
                    "principalId": "",
                    "principalType": "",
                    "sku": "standard"
                  },
                  "path": "appConfig.module.bicep",
                  "type": "azure.bicep.v0"
                },
                "blob": {
                  "connectionString": "{storage.outputs.blobEndpoint}",
                  "type": "value.v0"
                },
                "cosmos": {
                  "connectionString": "{cosmos.secretOutputs.connectionString}",
                  "params": {
                    "keyVaultName": ""
                  },
                  "path": "cosmos.module.bicep",
                  "type": "azure.bicep.v0"
                },
                "db": {
                  "connectionString": "{sql.connectionString};Database=db",
                  "type": "value.v0"
                },
                "db2": {
                  "connectionString": "{postgres2.connectionString};Database=db2",
                  "type": "value.v0"
                },
                "kv3": {
                  "connectionString": "{kv3.outputs.vaultUri}",
                  "params": {
                    "principalId": "",
                    "principalType": ""
                  },
                  "path": "kv3.module.bicep",
                  "type": "azure.bicep.v0"
                },
                "lawkspc": {
                  "path": "lawkspc.module.bicep",
                  "type": "azure.bicep.v0"
                },
                "postgres2": {
                  "connectionString": "{postgres2.secretOutputs.connectionString}",
                  "params": {
                    "administratorLogin": "{administratorLogin.value}",
                    "administratorLoginPassword": "{administratorLoginPassword.value}",
                    "keyVaultName": ""
                  },
                  "path": "postgres2.module.bicep",
                  "type": "azure.bicep.v0"
                },
                "queue": {
                  "connectionString": "{storage.outputs.queueEndpoint}",
                  "type": "value.v0"
                },
                "redis": {
                  "connectionString": "{redis.secretOutputs.connectionString}",
                  "params": {
                    "keyVaultName": ""
                  },
                  "path": "redis.module.bicep",
                  "type": "azure.bicep.v0"
                },
                "sb": {
                  "connectionString": "{sb.outputs.serviceBusEndpoint}",
                  "params": {
                    "principalId": "",
                    "principalType": ""
                  },
                  "path": "sb.module.bicep",
                  "type": "azure.bicep.v0"
                },
                "signalr": {
                  "connectionString": "Endpoint=https://{signalr.outputs.hostName};AuthType=azure",
                  "params": {
                    "principalId": "",
                    "principalType": ""
                  },
                  "path": "signalr.module.bicep",
                  "type": "azure.bicep.v0"
                },
                "sql": {
                  "connectionString": "Server=tcp:{sql.outputs.sqlServerFqdn},1433;Encrypt=True;Authentication=\u0022Active Directory Default\u0022",
                  "params": {
                    "principalId": "",
                    "principalName": ""
                  },
                  "path": "sql.module.bicep",
                  "type": "azure.bicep.v0"
                },
                "storage": {
                  "params": {
                    "principalId": "",
                    "principalType": ""
                  },
                  "path": "storage.module.bicep",
                  "type": "azure.bicep.v0"
                },
                "table": {
                  "connectionString": "{storage.outputs.tableEndpoint}",
                  "type": "value.v0"
                },
                "test": {
                  "params": {
                    "p2": "{test0.outputs.val0}",
                    "test": "{val.value}",
                    "values": ["one", "two"]
                  },
                  "path": "test.bicep",
                  "type": "azure.bicep.v0"
                },
                "test0": {
                  "path": "test0.module.bicep",
                  "type": "azure.bicep.v0"
                },
                "val": {
                  "inputs": {
                    "value": {
                      "type": "string"
                    }
                  },
                  "type": "parameter.v0",
                  "value": "{val.inputs.value}"
                }
              }
            }
            
            """;

        var manifestJson = JToken.Parse(manifestTest);
        var schema = await GetSchemaAsync();
        var isValid = manifestJson.IsValid(schema, out IList<ValidationError> errors);

        if (!isValid)
        {
            var errorMessages = errors.Select(e => e.Message).ToList();
            Assert.True(isValid, string.Join(Environment.NewLine, errorMessages));
        }
    }
}
