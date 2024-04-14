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
    public static IEnumerable<object[]> ApplicationSamples
    {
        get
        {
            yield return new[] { (IDistributedApplicationBuilder builder) =>
                {
                    builder.AddParameter("foo");
                }
            };

            yield return new[] { (IDistributedApplicationBuilder builder) =>
                {
                    builder.AddParameter("foo", secret: true);
                }
            };

            yield return new[] { (IDistributedApplicationBuilder builder) =>
                {
                    builder.AddConnectionString("foo");
                }
            };

            yield return new[] { (IDistributedApplicationBuilder builder) =>
                {
                    builder.AddRedis("redis");
                }
            };

            yield return new[] { (IDistributedApplicationBuilder builder) =>
                {
                    builder.AddProject<Projects.ServiceA>("project");
                }
            };

            yield return new[] { (IDistributedApplicationBuilder builder) =>
                {
                    builder.AddExecutable("executable", "hellworld", "foo", "arg1", "arg2");
                }
            };

            yield return new[] { (IDistributedApplicationBuilder builder) =>
                {
                    var awsSdkConfig = builder.AddAWSSDKConfig()
                                              .WithRegion(RegionEndpoint.USWest2)
                                              .WithProfile("test-profile");

                    builder.AddAWSCloudFormationStack("ExistingStack")
                           .WithReference(awsSdkConfig);
                }
            };

            yield return new[] { (IDistributedApplicationBuilder builder) =>
                {
                    var awsSdkConfig = builder.AddAWSSDKConfig()
                                              .WithRegion(RegionEndpoint.USWest2)
                                              .WithProfile("test-profile");

                    builder.AddAWSCloudFormationTemplate("TemplateStack", "nonexistenttemplate")
                           .WithReference(awsSdkConfig);
                }
            };

            yield return new[] { (IDistributedApplicationBuilder builder) =>
                {
                    var dapr = builder.AddDapr();
                    var state = dapr.AddDaprStateStore("daprstate");
                    var pubsub = dapr.AddDaprPubSub("daprpubsub");

                    builder.AddProject<Projects.ServiceA>("project")
                           .WithDaprSidecar()
                           .WithReference(state)
                           .WithReference(pubsub);
                }
            };
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

    [Theory]
    [MemberData(nameof(ApplicationSamples))]
    public async Task ValidateApplicationSamples(Action<IDistributedApplicationBuilder> configurator)
    {
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

    [Fact]
    public async Task SchemaRejectsUnspecifiedResourceType()
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
        Assert.False(manifestJson.IsValid(schema));
    }

    [Fact]
    public async Task SchemaWithContainerResourceWithMissingImageIsRejected()
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

    [Fact]
    public async Task SchemaWithContainerResourceAndNoEnvOrBindingsIsAccepted()
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

    [Fact]
    public async Task SchemaWithProjectResourceAndNoEnvOrBindingsIsAccepted()
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
