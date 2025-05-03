#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.AppService;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureAppServiceTests(ITestOutputHelper output)
{

    [Fact]
    public async Task AddContainerAppEnvironmentAddsDeploymentTargetWithContainerAppToProjectResources()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var env = builder.AddAppServiceEnvironment("env");

        builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpEndpoint()
            .WithExternalHttpEndpoints()
            .PublishAsAzureAppServiceWebsite((infrastructure, site) =>
            {
                site.SiteConfig.IsWebSocketsEnabled = true;
            });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.IsType<IComputeResource>(Assert.Single(model.GetProjectResources()), exactMatch: false);

        var target = container.GetDeploymentTargetAnnotation();

        Assert.NotNull(target);
        Assert.Same(env.Resource, target.ComputeEnvironment);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;
        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        var m = manifest.ToString();

        var expectedManifest =
        """
        {
          "type": "azure.bicep.v0",
          "path": "api.module.bicep",
          "params": {
            "env_outputs_azure_container_registry_endpoint": "{env.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT}",
            "env_outputs_id": "{env.outputs.id}",
            "env_outputs_azure_container_registry_managed_identity_id": "{env.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID}",
            "env_outputs_azure_container_registry_managed_identity_client_id": "{env.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_CLIENT_ID}",
            "api_containerimage": "{api.containerImage}",
            "api_containerport": "{api.containerPort}"
          }
        }
        """;

        Assert.Equal(expectedManifest, m);

        var expectedBicep =
        """
        @description('The location for the resource(s) to be deployed.')
        param location string = resourceGroup().location
        
        param env_outputs_azure_container_registry_endpoint string
        
        param env_outputs_id string
        
        param env_outputs_azure_container_registry_managed_identity_id string
        
        param env_outputs_azure_container_registry_managed_identity_client_id string
        
        param api_containerimage string
        
        param api_containerport string
        
        resource webapp 'Microsoft.Web/sites@2024-04-01' = {
          name: take('${toLower('api')}-${uniqueString(resourceGroup().id)}', 60)
          location: location
          properties: {
            serverFarmId: env_outputs_id
            siteConfig: {
              linuxFxVersion: 'DOCKER|${api_containerimage}'
              acrUseManagedIdentityCreds: true
              acrUserManagedIdentityID: env_outputs_azure_container_registry_managed_identity_client_id
              appSettings: [
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
              ]
              webSocketsEnabled: true
            }
          }
          identity: {
            type: 'UserAssigned'
            userAssignedIdentities: {
              '${env_outputs_azure_container_registry_managed_identity_id}': { }
            }
          }
        }
        """;
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task AddContainerAppEnvironmentAddsEnvironmentResource()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAppServiceEnvironment("env");

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var environment = Assert.Single(model.Resources.OfType<AzureAppServiceEnvironmentResource>());

        var (manifest, bicep) = await GetManifestWithBicep(environment);

        var m = manifest.ToString();

        var expectedManifest =
        """
        {
          "type": "azure.bicep.v0",
          "path": "env.module.bicep",
          "params": {
            "userPrincipalId": ""
          }
        }
        """;

        Assert.Equal(expectedManifest, m);

        string expectedBicep =
        """
        @description('The location for the resource(s) to be deployed.')
        param location string = resourceGroup().location
        
        param userPrincipalId string
        
        param tags object = { }
        
        resource env_mi 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
          name: take('env_mi-${uniqueString(resourceGroup().id)}', 128)
          location: location
        }
        
        resource env_acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
          name: take('envacr${uniqueString(resourceGroup().id)}', 50)
          location: location
          sku: {
            name: 'Basic'
          }
          tags: tags
        }
        
        resource env_acr_env_mi_AcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
          name: guid(env_acr.id, env_mi.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d'))
          properties: {
            principalId: env_mi.properties.principalId
            roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
            principalType: 'ServicePrincipal'
          }
          scope: env_acr
        }
        
        resource env_asplan 'Microsoft.Web/serverfarms@2024-04-01' = {
          name: take('envasplan-${uniqueString(resourceGroup().id)}', 60)
          location: location
          properties: {
            reserved: true
          }
          kind: 'Linux'
          sku: {
            name: 'B1'
            tier: 'Basic'
          }
        }
        
        output id string = env_asplan.id
        
        output AZURE_CONTAINER_REGISTRY_NAME string = env_acr.name
        
        output AZURE_CONTAINER_REGISTRY_ENDPOINT string = env_acr.properties.loginServer
        
        output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = env_mi.id
        
        output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_CLIENT_ID string = env_mi.properties.clientId
        """;

        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task KeyvaultReferenceHandling()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAppServiceEnvironment("env");

        var db = builder.AddAzureCosmosDB("mydb").WithAccessKeyAuthentication();
        db.AddCosmosDatabase("db");

        builder.AddProject<Project>("api", launchProfileName: null)
            .WithReference(db);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetProjectResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        var m = manifest.ToString();

        var expectedManifest =
        """
        {
          "type": "azure.bicep.v0",
          "path": "api.module.bicep",
          "params": {
            "env_outputs_azure_container_registry_endpoint": "{env.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT}",
            "env_outputs_id": "{env.outputs.id}",
            "env_outputs_azure_container_registry_managed_identity_id": "{env.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID}",
            "env_outputs_azure_container_registry_managed_identity_client_id": "{env.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_CLIENT_ID}",
            "api_containerimage": "{api.containerImage}",
            "mydb_kv_outputs_name": "{mydb-kv.outputs.name}",
            "api_identity_outputs_id": "{api-identity.outputs.id}",
            "api_identity_outputs_clientid": "{api-identity.outputs.clientId}"
          }
        }
        """;

        Assert.Equal(expectedManifest, m);

        var expectedBicep =
        """
        @description('The location for the resource(s) to be deployed.')
        param location string = resourceGroup().location
        
        param env_outputs_azure_container_registry_endpoint string
        
        param env_outputs_id string
        
        param env_outputs_azure_container_registry_managed_identity_id string
        
        param env_outputs_azure_container_registry_managed_identity_client_id string
        
        param api_containerimage string
        
        param mydb_kv_outputs_name string
        
        param api_identity_outputs_id string
        
        param api_identity_outputs_clientid string
        
        resource mydb_kv_outputs_name_kv 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
          name: mydb_kv_outputs_name
        }
        
        resource mydb_kv_outputs_name_kv_connectionstrings__mydb 'Microsoft.KeyVault/vaults/secrets@2023-07-01' existing = {
          name: 'connectionstrings--mydb'
          parent: mydb_kv_outputs_name_kv
        }
        
        resource webapp 'Microsoft.Web/sites@2024-04-01' = {
          name: take('${toLower('api')}-${uniqueString(resourceGroup().id)}', 60)
          location: location
          properties: {
            serverFarmId: env_outputs_id
            keyVaultReferenceIdentity: api_identity_outputs_id
            siteConfig: {
              linuxFxVersion: 'DOCKER|${api_containerimage}'
              acrUseManagedIdentityCreds: true
              acrUserManagedIdentityID: env_outputs_azure_container_registry_managed_identity_client_id
              appSettings: [
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
                  name: 'ConnectionStrings__mydb'
                  value: '@Microsoft.KeyVault(SecretUri=${mydb_kv_outputs_name_kv_connectionstrings__mydb.properties.secretUriWithVersion})'
                }
                {
                  name: 'AZURE_CLIENT_ID'
                  value: api_identity_outputs_clientid
                }
              ]
            }
          }
          identity: {
            type: 'UserAssigned'
            userAssignedIdentities: {
              '${env_outputs_azure_container_registry_managed_identity_id}': { }
              '${api_identity_outputs_id}': { }
            }
          }
        }
        """;
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task EndpointReferencesAreResolvedAcrossProjects()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAppServiceEnvironment("env");

        // Add 2 projects with endpoints
        var project1 = builder.AddProject<Project>("project1", launchProfileName: null)
            .WithHttpEndpoint()
            .WithExternalHttpEndpoints();

        var project2 = builder.AddProject<Project>("project2", launchProfileName: null)
            .WithHttpEndpoint()
            .WithExternalHttpEndpoints()
            .WithReference(project1);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        
        project2.Resource.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        var m = manifest.ToString();

        var expectedManifest =
        """
        {
          "type": "azure.bicep.v0",
          "path": "project2.module.bicep",
          "params": {
            "env_outputs_azure_container_registry_endpoint": "{env.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT}",
            "env_outputs_id": "{env.outputs.id}",
            "env_outputs_azure_container_registry_managed_identity_id": "{env.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID}",
            "env_outputs_azure_container_registry_managed_identity_client_id": "{env.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_CLIENT_ID}",
            "project2_containerimage": "{project2.containerImage}",
            "project2_containerport": "{project2.containerPort}"
          }
        }
        """;

        Assert.Equal(expectedManifest, m);

        var expectedBicep =
        """
        @description('The location for the resource(s) to be deployed.')
        param location string = resourceGroup().location

        param env_outputs_azure_container_registry_endpoint string

        param env_outputs_id string

        param env_outputs_azure_container_registry_managed_identity_id string

        param env_outputs_azure_container_registry_managed_identity_client_id string

        param project2_containerimage string

        param project2_containerport string

        resource webapp 'Microsoft.Web/sites@2024-04-01' = {
          name: take('${toLower('project2')}-${uniqueString(resourceGroup().id)}', 60)
          location: location
          properties: {
            serverFarmId: env_outputs_id
            siteConfig: {
              linuxFxVersion: 'DOCKER|${project2_containerimage}'
              acrUseManagedIdentityCreds: true
              acrUserManagedIdentityID: env_outputs_azure_container_registry_managed_identity_client_id
              appSettings: [
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
                  value: project2_containerport
                }
                {
                  name: 'services__project1__http__0'
                  value: 'http://${take('${toLower('project1')}-${uniqueString(resourceGroup().id)}', 60)}.azurewebsites.net'
                }
              ]
            }
          }
          identity: {
            type: 'UserAssigned'
            userAssignedIdentities: {
              '${env_outputs_azure_container_registry_managed_identity_id}': { }
            }
          }
        }
        """;

        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    private static Task<(JsonNode ManifestNode, string BicepText)> GetManifestWithBicep(IResource resource) =>
        AzureManifestUtils.GetManifestWithBicep(resource, skipPreparer: true);

    private sealed class Project : IProjectMetadata
    {
        public string ProjectPath => "project";
    }
}