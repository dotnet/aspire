// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Azure.Tests;

public class AzureContainerAppsTests
{
    [Fact]
    public async Task AddContainerAppsInfrastructureAddsDeploymentTargetWithContainerAppToContainerResources()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddContainerAppsInfrastructure();

        builder.AddContainer("api", "myimage");

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureConstructResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await ManifestUtils.GetManifestWithBicep(resource);

        var m = manifest.ToString();

        var expectedManifest =
        """
        {
          "type": "azure.bicep.v0",
          "path": "api.module.bicep",
          "params": {
            "outputs_azure_container_registry_managed_identity_id": "{.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID}",
            "outputs_managed_identity_client_id": "{.outputs.MANAGED_IDENTITY_CLIENT_ID}",
            "outputs_azure_container_apps_environment_id": "{.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID}"
          }
        }
        """;

        Assert.Equal(expectedManifest, m);

        var expectedBicep =
        """
        @description('The location for the resource(s) to be deployed.')
        param location string = resourceGroup().location

        param outputs_azure_container_registry_managed_identity_id string

        param outputs_managed_identity_client_id string

        param outputs_azure_container_apps_environment_id string

        resource api 'Microsoft.App/containerApps@2024-03-01' = {
          name: 'api'
          location: location
          properties: {
            configuration: {
              activeRevisionsMode: 'Single'
            }
            environmentId: outputs_azure_container_apps_environment_id
            template: {
              containers: [
                {
                  image: 'myimage:latest'
                  name: 'api'
                  env: [
                    {
                      name: 'AZURE_CLIENT_ID'
                      value: outputs_managed_identity_client_id
                    }
                  ]
                }
              ]
              scale: {
                minReplicas: 1
              }
            }
          }
          identity: {
            type: 'UserAssigned'
            userAssignedIdentities: {
              '${outputs_azure_container_registry_managed_identity_id}': { }
            }
          }
        }
        """;

        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task AddContainerAppsInfrastructureAddsDeploymentTargetWithContainerAppToProjectResources()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddContainerAppsInfrastructure();

        builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpEndpoint();

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetProjectResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureConstructResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await ManifestUtils.GetManifestWithBicep(resource);

        var m = manifest.ToString();

        var expectedManifest =
        """
        {
          "type": "azure.bicep.v0",
          "path": "api.module.bicep",
          "params": {
            "api_containerport": "{api.containerPort}",
            "outputs_azure_container_registry_managed_identity_id": "{.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID}",
            "outputs_managed_identity_client_id": "{.outputs.MANAGED_IDENTITY_CLIENT_ID}",
            "outputs_azure_container_apps_environment_id": "{.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID}",
            "outputs_azure_container_registry_endpoint": "{.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT}",
            "api_containerimage": "{api.containerImage}"
          }
        }
        """;

        Assert.Equal(expectedManifest, m);

        var expectedBicep =
        """
        @description('The location for the resource(s) to be deployed.')
        param location string = resourceGroup().location

        param api_containerport string

        param outputs_azure_container_registry_managed_identity_id string

        param outputs_managed_identity_client_id string

        param outputs_azure_container_apps_environment_id string

        param outputs_azure_container_registry_endpoint string

        param api_containerimage string

        resource api 'Microsoft.App/containerApps@2024-03-01' = {
          name: 'api'
          location: location
          properties: {
            configuration: {
              activeRevisionsMode: 'Single'
              ingress: {
                external: false
                targetPort: api_containerport
                transport: 'http'
              }
              registries: [
                {
                  server: outputs_azure_container_registry_endpoint
                  identity: outputs_azure_container_registry_managed_identity_id
                }
              ]
            }
            environmentId: outputs_azure_container_apps_environment_id
            template: {
              containers: [
                {
                  image: api_containerimage
                  name: 'api'
                  env: [
                    {
                      name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES'
                      value: 'true'
                    }
                    {
                      name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES'
                      value: 'true'
                    }
                    {
                      name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY'
                      value: 'in_memory'
                    }
                    {
                      name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED'
                      value: 'true'
                    }
                    {
                      name: 'HTTP_PORTS'
                      value: api_containerport
                    }
                    {
                      name: 'AZURE_CLIENT_ID'
                      value: outputs_managed_identity_client_id
                    }
                  ]
                }
              ]
              scale: {
                minReplicas: 1
              }
            }
          }
          identity: {
            type: 'UserAssigned'
            userAssignedIdentities: {
              '${outputs_azure_container_registry_managed_identity_id}': { }
            }
          }
        }
        """;

        Assert.Equal(expectedBicep, bicep);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ExecuteBeforeStartHooksAsync")]
    private static extern Task ExecuteBeforeStartHooksAsync(DistributedApplication app, CancellationToken cancellationToken);

    private sealed class Project : IProjectMetadata
    {
        public string ProjectPath => "project";
    }
}
