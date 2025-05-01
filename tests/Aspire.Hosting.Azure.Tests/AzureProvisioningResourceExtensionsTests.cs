// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Azure.Provisioning.AppContainers;
using Xunit;

namespace Aspire.Hosting.Azure.Tests;

public class AzureProvisioningResourceExtensionsTests(ITestOutputHelper output)
{
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

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param endpointAddressParam string

            param someExpressionParam string

            resource app 'Microsoft.App/containerApps@2024-03-01' = {
              name: take('app-${uniqueString(resourceGroup().id)}', 32)
              location: location
              properties: {
                template: {
                  scale: {
                    rules: [
                      {
                        name: 'temp'
                        custom: {
                          type: 'external'
                          metadata: {
                            address: endpointAddressParam
                            someExpression: someExpressionParam
                          }
                        }
                      }
                    ]
                  }
                }
              }
            }
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    private sealed class Project : IProjectMetadata
    {
        public string ProjectPath => "project";
    }
}
