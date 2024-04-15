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
}
