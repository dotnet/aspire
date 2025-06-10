// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Utils;
using Azure.Provisioning;
using Azure.Provisioning.Roles;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Azure.Tests;

public class AzureBicepResourceTests
{
    [Fact]
    public void AddBicepResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bicepResource = builder.AddBicepTemplateString("mytemplate", "content")
                                   .WithParameter("param1", "value1")
                                   .WithParameter("param2", "value2");

        Assert.Equal("content", bicepResource.Resource.TemplateString);
        Assert.Equal("value1", bicepResource.Resource.Parameters["param1"]);
        Assert.Equal("value2", bicepResource.Resource.Parameters["param2"]);
    }

    public static TheoryData<Func<IDistributedApplicationBuilder, IResourceBuilder<IResource>>> AzureExtensions =>
        CreateAllAzureExtensions("x");

    private static TheoryData<Func<IDistributedApplicationBuilder, IResourceBuilder<IResource>>> CreateAllAzureExtensions(string resourceName)
    {
        static void CreateInfrastructure(AzureResourceInfrastructure infrastructure)
        {
            var id = new UserAssignedIdentity("id");
            infrastructure.Add(id);
            infrastructure.Add(new ProvisioningOutput("cid", typeof(string)) { Value = id.ClientId });
        }

        return new()
        {
            { builder => builder.AddAzureAppConfiguration(resourceName) },
            { builder => builder.AddAzureApplicationInsights(resourceName) },
            { builder => builder.AddBicepTemplate(resourceName, "template.bicep") },
            { builder => builder.AddBicepTemplateString(resourceName, "content") },
            { builder => builder.AddAzureInfrastructure(resourceName, CreateInfrastructure) },
            { builder => builder.AddAzureOpenAI(resourceName) },
            { builder => builder.AddAzureCosmosDB(resourceName) },
            { builder => builder.AddAzureEventHubs(resourceName) },
            { builder => builder.AddAzureKeyVault(resourceName) },
            { builder => builder.AddAzureLogAnalyticsWorkspace(resourceName) },
#pragma warning disable CS0618 // Type or member is obsolete
            { builder => builder.AddPostgres(resourceName).AsAzurePostgresFlexibleServer() },
            { builder => builder.AddRedis(resourceName).AsAzureRedis() },
            { builder => builder.AddSqlServer(resourceName).AsAzureSqlDatabase() },
#pragma warning restore CS0618 // Type or member is obsolete
            { builder => builder.AddAzurePostgresFlexibleServer(resourceName) },
            { builder => builder.AddAzureRedis(resourceName) },
            { builder => builder.AddAzureSearch(resourceName) },
            { builder => builder.AddAzureServiceBus(resourceName) },
            { builder => builder.AddAzureSignalR(resourceName) },
            { builder => builder.AddAzureSqlServer(resourceName) },
            { builder => builder.AddAzureStorage(resourceName) },
            { builder => builder.AddAzureWebPubSub(resourceName) },
        };
    }

    [Theory]
    [MemberData(nameof(AzureExtensions))]
    public void AzureExtensionsAutomaticallyAddAzureProvisioning(Func<IDistributedApplicationBuilder, IResourceBuilder<IResource>> addAzureResource)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        addAzureResource(builder);

        var app = builder.Build();
        var hooks = app.Services.GetServices<IDistributedApplicationLifecycleHook>();
        Assert.Single(hooks.OfType<AzureProvisioner>());
    }

    [Theory]
    [MemberData(nameof(AzureExtensions))]
    public void BicepResourcesAreIdempotent(Func<IDistributedApplicationBuilder, IResourceBuilder<IResource>> addAzureResource)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var azureResourceBuilder = addAzureResource(builder);

        if (azureResourceBuilder.Resource is not AzureProvisioningResource bicepResource)
        {
            // Skip
            return;
        }

        // This makes sure that these don't throw
        bicepResource.GetBicepTemplateFile();
        bicepResource.GetBicepTemplateFile();
    }

    public static TheoryData<Func<IDistributedApplicationBuilder, IResourceBuilder<IResource>>> AzureExtensionsWithHyphen =>
        CreateAllAzureExtensions("x-y");

    [Theory]
    [MemberData(nameof(AzureExtensionsWithHyphen))]
    public void AzureResourcesProduceValidBicep(Func<IDistributedApplicationBuilder, IResourceBuilder<IResource>> addAzureResource)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var azureResourceBuilder = addAzureResource(builder);

        if (azureResourceBuilder.Resource is not AzureProvisioningResource bicepResource)
        {
            // Skip
            return;
        }

        var bicep = bicepResource.GetBicepTemplateString();

        Assert.DoesNotContain("resource x-y", bicep);
    }

    [Fact]
    public void GetOutputReturnsOutputValue()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bicepResource = builder.AddBicepTemplateString("templ", "content");

        bicepResource.Resource.Outputs["resourceEndpoint"] = "https://myendpoint";

        Assert.Equal("https://myendpoint", bicepResource.GetOutput("resourceEndpoint").Value);
    }

    [Fact]
    public void GetSecretOutputReturnsSecretOutputValue()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bicepResource = builder.AddBicepTemplateString("templ", "content");

        bicepResource.Resource.SecretOutputs["connectionString"] = "https://myendpoint;Key=43";

#pragma warning disable CS0618 // Type or member is obsolete
        Assert.Equal("https://myendpoint;Key=43", bicepResource.GetSecretOutput("connectionString").Value);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [Fact]
    public void GetOutputValueThrowsIfNoOutput()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bicepResource = builder.AddBicepTemplateString("templ", "content");

        Assert.Throws<InvalidOperationException>(() => bicepResource.GetOutput("resourceEndpoint").Value);
    }

    [Fact]
    public void GetSecretOutputValueThrowsIfNoOutput()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bicepResource = builder.AddBicepTemplateString("templ", "content");

#pragma warning disable CS0618 // Type or member is obsolete
        Assert.Throws<InvalidOperationException>(() => bicepResource.GetSecretOutput("connectionString").Value);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [Fact]
    public async Task AssertManifestLayout()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var param = builder.AddParameter("p1");

        var b2 = builder.AddBicepTemplateString("temp2", "content");

        var bicepResource = builder.AddBicepTemplateString("templ", "content")
                                    .WithParameter("param1", "value1")
                                    .WithParameter("param2", ["1", "2"])
                                    .WithParameter("param3", new JsonObject() { ["value"] = "nested" })
                                    .WithParameter("param4", param)
                                    .WithParameter("param5", b2.GetOutput("value1"))
                                    .WithParameter("param6", () => b2.GetOutput("value2"));

        bicepResource.Resource.TempDirectory = Environment.CurrentDirectory;

        var manifest = await ManifestUtils.GetManifest(bicepResource.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "path": "templ.module.bicep",
              "params": {
                "param1": "value1",
                "param2": [
                  "1",
                  "2"
                ],
                "param3": {
                  "value": "nested"
                },
                "param4": "{p1.value}",
                "param5": "{temp2.outputs.value1}",
                "param6": "{temp2.outputs.value2}"
              }
            }
            """;

        Assert.Equal(expectedManifest, manifest.ToString());
    }
}
