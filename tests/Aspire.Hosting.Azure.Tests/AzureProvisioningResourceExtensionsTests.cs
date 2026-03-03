// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Azure.Provisioning;
using Azure.Provisioning.AppContainers;

namespace Aspire.Hosting.Azure.Tests;

public class AzureProvisioningResourceExtensionsTests
{
    [Fact]
    public async Task ConfigureInfrastructureJson_InvokesCallbackAndBuildsBicep()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var callbackInvoked = false;

        var infrastructureResource = builder.AddAzureInfrastructure("resource", infrastructure =>
            {
                infrastructure.Add(new ProvisioningParameter("myValue", typeof(string))
                {
                    Value = "before"
                });
            })
            .ConfigureInfrastructureJson(payload =>
            {
                callbackInvoked = true;
                Assert.NotEmpty(payload);
                return payload;
            });

        var manifest = await AzureManifestUtils.GetManifestWithBicep(infrastructureResource.Resource);

        Assert.True(callbackInvoked);
        Assert.Contains("param myValue string", manifest.BicepText, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfigureInfrastructureJson_ThrowsForUnknownProperty()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var infrastructureResource = builder.AddAzureInfrastructure("resource", infrastructure =>
            {
                infrastructure.Add(new ProvisioningParameter("myValue", typeof(string))
                {
                    Value = "before"
                });
            })
            .ConfigureInfrastructureJson(payload =>
            {
                var resources = JsonNode.Parse(payload)?.AsArray() ?? [];
                resources.Add(new JsonObject
                {
                    ["bicepIdentifier"] = "missing-resource",
                    ["properties"] = new JsonObject()
                });

                return resources.ToJsonString();
            });

        var exception = Assert.Throws<InvalidOperationException>(infrastructureResource.Resource.GetBicepTemplateString);

        Assert.Contains("missing-resource", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfigureInfrastructure_HasAspireExportIgnoreAttribute()
    {
        var method = typeof(AzureProvisioningResourceExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(m => m.Name == nameof(AzureProvisioningResourceExtensions.ConfigureInfrastructure)
                && m.GetParameters().Length == 2
                && m.GetParameters()[1].ParameterType == typeof(Action<global::Aspire.Hosting.Azure.AzureResourceInfrastructure>));

        var ignoreAttribute = method.GetCustomAttribute<AspireExportIgnoreAttribute>();

        Assert.NotNull(ignoreAttribute);
    }

    [Fact]
    public void ConfigureInfrastructureJson_HasAspireExportAttribute()
    {
        var method = typeof(AzureProvisioningResourceExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(AzureProvisioningResourceExtensions.ConfigureInfrastructureJson)
                && m.GetParameters().Length == 2
                && m.GetParameters()[1].ParameterType == typeof(Func<string, string>));

        var exportAttribute = method.GetCustomAttribute<AspireExportAttribute>();

        Assert.NotNull(exportAttribute);
        Assert.Equal("configureInfrastructure", exportAttribute.MethodName);
    }

    [Fact]
    public async Task AsProvisioningParameterTests()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var apiProject = builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpsEndpoint();

        var endpointReference = apiProject.GetEndpoint("https");
        var referenceExpression = ReferenceExpression.Create($"prefix:{endpointReference.Property(EndpointProperty.HostAndPort)}");

        var resource1 = builder.AddAzureInfrastructure("resource1", infrastructure =>
        {
            var endpointAddressParam = endpointReference.AsProvisioningParameter(infrastructure, parameterName: "endpointAddressParam");
            var someExpressionParam = referenceExpression.AsProvisioningParameter(infrastructure, parameterName: "someExpressionParam");

            var app = new ContainerApp("app");
            app.Template.Scale.Rules =
            [
                new ContainerAppScaleRule()
                {
                    Name = "temp",
                    Custom = new ContainerAppCustomScaleRule()
                    {
                        CustomScaleRuleType= "external",
                        Metadata =
                        {
                            { "address", endpointAddressParam },
                            { "someExpression", someExpressionParam },
                        }
                    }
                }
            ];
            infrastructure.Add(app);
        });

        var manifest = await AzureManifestUtils.GetManifestWithBicep(resource1.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "path": "resource1.module.bicep",
              "params": {
                "endpointAddressParam": "{api.bindings.https.url}",
                "someExpressionParam": "prefix:{api.bindings.https.host}:{api.bindings.https.port}"
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        await Verify(manifest.BicepText, extension: "bicep");
            
    }

    private sealed class Project : IProjectMetadata
    {
        public string ProjectPath => "project";
    }
}
