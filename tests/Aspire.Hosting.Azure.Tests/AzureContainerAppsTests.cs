// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREACADOMAINS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Azure.Provisioning;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Azure.Tests;

public class AzureContainerAppsTests(ITestOutputHelper output)
{
    [Fact]
    public async Task AddContainerAppsInfrastructureAddsDeploymentTargetWithContainerAppToContainerResources()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppsInfrastructure();

        builder.AddContainer("api", "myimage");

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

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
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task AddDockerfileWithAppsInfrastructureAddsDeploymentTargetWithContainerAppToContainerResources()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppsInfrastructure();

        var directory = Directory.CreateTempSubdirectory(".aspire-test");

        // Contents of the Dockerfile are not important for this test
        File.WriteAllText(Path.Combine(directory.FullName, "Dockerfile"), "");

        builder.AddDockerfile("api", directory.FullName);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

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
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task AddContainerAppsInfrastructureAddsDeploymentTargetWithContainerAppToProjectResources()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppsInfrastructure();

        builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpEndpoint();

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetProjectResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

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
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task AddContainerAppsInfrastructureWithParameterReference()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppsInfrastructure();

        var value = builder.AddParameter("value");
        var minReplicas = builder.AddParameter("minReplicas");

        builder.AddContainer("api", "myimage")
               .PublishAsAzureContainerApp((module, c) =>
               {
                   var val = new ContainerAppEnvironmentVariable()
                   {
                       Name = "Parameter",
                       Value = value.AsProvisioningParameter(module)
                   };

                   c.Template.Containers[0].Value!.Env.Add(val);
                   c.Template.Scale.MinReplicas = minReplicas.AsProvisioningParameter(module);
               });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

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
            "outputs_azure_container_apps_environment_id": "{.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID}",
            "value": "{value.value}",
            "minReplicas": "{minReplicas.value}"
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

        param value string

        param minReplicas string

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
                    {
                      name: 'Parameter'
                      value: value
                    }
                  ]
                }
              ]
              scale: {
                minReplicas: minReplicas
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

        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task ProjectWithManyReferenceTypes()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppsInfrastructure();

        // CosmosDB uses secret outputs
        var db = builder.AddAzureCosmosDB("mydb").AddDatabase("db");

        // Postgres uses secret outputs + a literal connection string
        var pgdb = builder.AddAzurePostgresFlexibleServer("pg").WithPasswordAuthentication().AddDatabase("db");

        // Connection string (should be considered a secret)
        var blob = builder.AddAzureStorage("storage").AddBlobs("blobs");

        // Secret parameters (_ isn't supported and will be replaced by -)
        var secretValue = builder.AddParameter("value0", "x", secret: true);

        // Normal parameters
        var value = builder.AddParameter("value1", "y");

        var project = builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpEndpoint()
            .WithHttpsEndpoint()
            .WithHttpEndpoint(name: "internal")
            .WithReference(db)
            .WithReference(blob)
            .WithReference(pgdb)
            .WithEnvironment("SecretVal", secretValue)
            .WithEnvironment("secret_value_1", secretValue)
            .WithEnvironment("Value", value);

        project.WithEnvironment(context =>
        {
            var httpEp = project.GetEndpoint("http");
            var httpsEp = project.GetEndpoint("https");
            var internalEp = project.GetEndpoint("internal");

            context.EnvironmentVariables["HTTP_EP"] = project.GetEndpoint("http");
            context.EnvironmentVariables["HTTPS_EP"] = project.GetEndpoint("https");
            context.EnvironmentVariables["INTERNAL_EP"] = project.GetEndpoint("internal");
            context.EnvironmentVariables["TARGET_PORT"] = httpEp.Property(EndpointProperty.TargetPort);
            context.EnvironmentVariables["PORT"] = httpEp.Property(EndpointProperty.Port);
            context.EnvironmentVariables["HOST"] = httpEp.Property(EndpointProperty.Host);
            context.EnvironmentVariables["SCHEME"] = httpEp.Property(EndpointProperty.Scheme);
        });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetProjectResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

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
            "mydb_secretoutputs": "{mydb.secretOutputs}",
            "outputs_azure_container_registry_managed_identity_id": "{.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID}",
            "storage_outputs_blobendpoint": "{storage.outputs.blobEndpoint}",
            "pg_secretoutputs": "{pg.secretOutputs}",
            "value0_value": "{value0.value}",
            "value1_value": "{value1.value}",
            "outputs_azure_container_apps_environment_default_domain": "{.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}",
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

        param mydb_secretoutputs string

        param outputs_azure_container_registry_managed_identity_id string

        param storage_outputs_blobendpoint string

        param pg_secretoutputs string

        @secure()
        param value0_value string

        param value1_value string

        param outputs_azure_container_apps_environment_default_domain string

        param outputs_managed_identity_client_id string

        param outputs_azure_container_apps_environment_id string

        param outputs_azure_container_registry_endpoint string

        param api_containerimage string

        resource mydb_secretoutputs_kv 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
          name: mydb_secretoutputs
        }

        resource pg_secretoutputs_kv 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
          name: pg_secretoutputs
        }

        resource mydb_secretoutputs_kv_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' existing = {
          name: 'connectionString'
          parent: mydb_secretoutputs_kv
        }

        resource pg_secretoutputs_kv_db_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' existing = {
          name: 'db-connectionString'
          parent: pg_secretoutputs_kv
        }

        resource api 'Microsoft.App/containerApps@2024-03-01' = {
          name: 'api'
          location: location
          properties: {
            configuration: {
              secrets: [
                {
                  name: 'connectionstrings--mydb'
                  identity: outputs_azure_container_registry_managed_identity_id
                  keyVaultUrl: mydb_secretoutputs_kv_connectionString.properties.secretUri
                }
                {
                  name: 'connectionstrings--db'
                  identity: outputs_azure_container_registry_managed_identity_id
                  keyVaultUrl: pg_secretoutputs_kv_db_connectionString.properties.secretUri
                }
                {
                  name: 'secretval'
                  value: value0_value
                }
                {
                  name: 'secret-value-1'
                  value: value0_value
                }
              ]
              activeRevisionsMode: 'Single'
              ingress: {
                external: false
                targetPort: api_containerport
                transport: 'http'
                additionalPortMappings: [
                  {
                    external: false
                    targetPort: 8000
                  }
                ]
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
                      value: '${api_containerport};8000'
                    }
                    {
                      name: 'HTTPS_PORTS'
                      value: api_containerport
                    }
                    {
                      name: 'ConnectionStrings__mydb'
                      secretRef: 'connectionstrings--mydb'
                    }
                    {
                      name: 'ConnectionStrings__blobs'
                      value: storage_outputs_blobendpoint
                    }
                    {
                      name: 'ConnectionStrings__db'
                      secretRef: 'connectionstrings--db'
                    }
                    {
                      name: 'SecretVal'
                      secretRef: 'secretval'
                    }
                    {
                      name: 'secret_value_1'
                      secretRef: 'secret-value-1'
                    }
                    {
                      name: 'Value'
                      value: value1_value
                    }
                    {
                      name: 'HTTP_EP'
                      value: 'http://api.internal.${outputs_azure_container_apps_environment_default_domain}'
                    }
                    {
                      name: 'HTTPS_EP'
                      value: 'https://api.internal.${outputs_azure_container_apps_environment_default_domain}'
                    }
                    {
                      name: 'INTERNAL_EP'
                      value: 'http://api:8000'
                    }
                    {
                      name: 'TARGET_PORT'
                      value: api_containerport
                    }
                    {
                      name: 'PORT'
                      value: '80'
                    }
                    {
                      name: 'HOST'
                      value: 'api.internal.${outputs_azure_container_apps_environment_default_domain}'
                    }
                    {
                      name: 'SCHEME'
                      value: 'http'
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
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task PublishAsContainerAppInfluencesContainerAppDefinition()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppsInfrastructure();
        builder.AddContainer("api", "myimage")
            .PublishAsAzureContainerApp((module, c) =>
            {
                Assert.Contains(c, module.GetProvisionableResources());

                c.Template.Scale.MinReplicas = 0;
            });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

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
                minReplicas: 0
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
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task ConfigureCustomDomainsMutatesIngress()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var customDomain = builder.AddParameter("customDomain");
        var certificateName = builder.AddParameter("certificateName");

        builder.AddAzureContainerAppsInfrastructure();
        builder.AddContainer("api", "myimage")
            .WithHttpEndpoint(targetPort: 1111)
            .PublishAsAzureContainerApp((module, c) =>
            {
                c.ConfigureCustomDomain(customDomain, certificateName);
            });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureBicepResource;

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
            "outputs_azure_container_apps_environment_id": "{.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID}",
            "certificateName": "{certificateName.value}",
            "customDomain": "{customDomain.value}"
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

        param certificateName string

        param customDomain string

        resource api 'Microsoft.App/containerApps@2024-03-01' = {
          name: 'api'
          location: location
          properties: {
            configuration: {
              activeRevisionsMode: 'Single'
              ingress: {
                external: false
                targetPort: 1111
                transport: 'http'
                customDomains: [
                  {
                    name: customDomain
                    bindingType: (certificateName != '') ? 'SniEnabled' : 'Disabled'
                    certificateId: (certificateName != '') ? '${outputs_azure_container_apps_environment_id}/managedCertificates/${certificateName}' : null
                  }
                ]
              }
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
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task VolumesAndBindMountsAreTranslation()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppsInfrastructure();

        builder.AddContainer("api", "myimage")
            .WithVolume("vol1", "/path1")
            .WithVolume("vol2", "/path2")
            .WithBindMount("bind1", "/path3");

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await ManifestUtils.GetManifestWithBicep(resource);

        var m = manifest.ToString();

        var expectedManifest =
        """
        {
          "type": "azure.bicep.v0",
          "path": "api.module.bicep",
          "params": {
            "api_volumes_0_storage": "{api.volumes.0.storage}",
            "api_volumes_1_storage": "{api.volumes.1.storage}",
            "api_bindmounts_0_storage": "{api.bindMounts.0.storage}",
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

        param api_volumes_0_storage string

        param api_volumes_1_storage string

        param api_bindmounts_0_storage string

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
                  volumeMounts: [
                    {
                      volumeName: 'v0'
                      mountPath: '/path1'
                    }
                    {
                      volumeName: 'v1'
                      mountPath: '/path2'
                    }
                    {
                      volumeName: 'bm0'
                      mountPath: '/path3'
                    }
                  ]
                }
              ]
              scale: {
                minReplicas: 1
              }
              volumes: [
                {
                  name: 'v0'
                  storageType: 'AzureFile'
                  storageName: api_volumes_0_storage
                }
                {
                  name: 'v1'
                  storageType: 'AzureFile'
                  storageName: api_volumes_1_storage
                }
                {
                  name: 'bm0'
                  storageType: 'AzureFile'
                  storageName: api_bindmounts_0_storage
                }
              ]
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
    public async Task SecretOutputHandling()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppsInfrastructure();

        var db = builder.AddAzureCosmosDB("mydb").AddDatabase("db");

        builder.AddContainer("api", "image")
            .WithReference(db)
            .WithEnvironment(context =>
            {
                // Any value that resolves to the secret output can be a direct keyvault reference.
                // This includes nested expressions.
                var connectionString = db.GetSecretOutput("connectionString");
                var secret0 = ReferenceExpression.Create($"{connectionString}");
                var secret1 = ReferenceExpression.Create($"{secret0}");

                context.EnvironmentVariables["connectionString"] = connectionString;
                context.EnvironmentVariables["secret0"] = secret0;
                context.EnvironmentVariables["secret1"] = secret1;

                var connectionString1 = db.GetSecretOutput("connectionString1");
                // Complex expressions that contain a secret output
                var complex = ReferenceExpression.Create($"a/{connectionString}/{secret0}/{connectionString1}");
                context.EnvironmentVariables["complex"] = complex;
            });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await ManifestUtils.GetManifestWithBicep(resource);

        var m = manifest.ToString();

        var expectedManifest =
        """
        {
          "type": "azure.bicep.v0",
          "path": "api.module.bicep",
          "params": {
            "mydb_secretoutputs": "{mydb.secretOutputs}",
            "outputs_azure_container_registry_managed_identity_id": "{.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID}",
            "mydb_secretoutputs_connectionstring": "{mydb.secretOutputs.connectionString}",
            "mydb_secretoutputs_connectionstring1": "{mydb.secretOutputs.connectionString1}",
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

        param mydb_secretoutputs string

        param outputs_azure_container_registry_managed_identity_id string

        @secure()
        param mydb_secretoutputs_connectionstring string

        @secure()
        param mydb_secretoutputs_connectionstring1 string

        param outputs_managed_identity_client_id string

        param outputs_azure_container_apps_environment_id string

        resource mydb_secretoutputs_kv 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
          name: mydb_secretoutputs
        }

        resource mydb_secretoutputs_kv_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' existing = {
          name: 'connectionString'
          parent: mydb_secretoutputs_kv
        }

        resource api 'Microsoft.App/containerApps@2024-03-01' = {
          name: 'api'
          location: location
          properties: {
            configuration: {
              secrets: [
                {
                  name: 'connectionstrings--mydb'
                  identity: outputs_azure_container_registry_managed_identity_id
                  keyVaultUrl: mydb_secretoutputs_kv_connectionString.properties.secretUri
                }
                {
                  name: 'connectionstring'
                  identity: outputs_azure_container_registry_managed_identity_id
                  keyVaultUrl: mydb_secretoutputs_kv_connectionString.properties.secretUri
                }
                {
                  name: 'secret0'
                  identity: outputs_azure_container_registry_managed_identity_id
                  keyVaultUrl: mydb_secretoutputs_kv_connectionString.properties.secretUri
                }
                {
                  name: 'secret1'
                  identity: outputs_azure_container_registry_managed_identity_id
                  keyVaultUrl: mydb_secretoutputs_kv_connectionString.properties.secretUri
                }
                {
                  name: 'complex'
                  value: 'a/${mydb_secretoutputs_connectionstring}/${mydb_secretoutputs_connectionstring}/${mydb_secretoutputs_connectionstring1}'
                }
              ]
              activeRevisionsMode: 'Single'
            }
            environmentId: outputs_azure_container_apps_environment_id
            template: {
              containers: [
                {
                  image: 'image:latest'
                  name: 'api'
                  env: [
                    {
                      name: 'ConnectionStrings__mydb'
                      secretRef: 'connectionstrings--mydb'
                    }
                    {
                      name: 'connectionString'
                      secretRef: 'connectionstring'
                    }
                    {
                      name: 'secret0'
                      secretRef: 'secret0'
                    }
                    {
                      name: 'secret1'
                      secretRef: 'secret1'
                    }
                    {
                      name: 'complex'
                      secretRef: 'complex'
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
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task CanCustomizeWithProvisioningBuildOptions()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.Services.Configure<AzureProvisioningOptions>(options => options.ProvisioningBuildOptions.InfrastructureResolvers.Insert(0, new MyResourceNamePropertyResolver()));
        builder.AddAzureContainerAppsInfrastructure();

        builder.AddContainer("api1", "myimage");

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (_, bicep) = await ManifestUtils.GetManifestWithBicep(resource);

        var expectedBicep =
        """
        @description('The location for the resource(s) to be deployed.')
        param location string = resourceGroup().location

        param outputs_azure_container_registry_managed_identity_id string

        param outputs_managed_identity_client_id string

        param outputs_azure_container_apps_environment_id string

        resource api1 'Microsoft.App/containerApps@2024-03-01' = {
          name: 'api1-my'
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
                  name: 'api1'
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
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    private sealed class MyResourceNamePropertyResolver : DynamicResourceNamePropertyResolver
    {
        public override void ResolveProperties(ProvisionableConstruct construct, ProvisioningBuildOptions options)
        {
            if (construct is ContainerApp app)
            {
                app.Name = app.Name.Value + "-my";
            }

            base.ResolveProperties(construct, options);
        }
    }

    [Fact]
    public async Task ExternalEndpointBecomesIngress()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppsInfrastructure();

        builder.AddContainer("api", "myimage")
            .WithHttpEndpoint()
            .WithExternalHttpEndpoints();

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

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
              ingress: {
                external: true
                targetPort: 8000
                transport: 'http'
              }
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
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task FirstHttpEndpointBecomesIngress()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppsInfrastructure();

        builder.AddContainer("api", "myimage")
            .WithHttpEndpoint(name: "one", targetPort: 8080)
            .WithHttpEndpoint(name: "two", targetPort: 8081);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

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
              ingress: {
                external: false
                targetPort: 8080
                transport: 'http'
                additionalPortMappings: [
                  {
                    external: false
                    targetPort: 8081
                  }
                ]
              }
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
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task EndpointWithHttp2SetsTransportToH2()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppsInfrastructure();

        builder.AddContainer("api", "myimage")
            .WithHttpEndpoint()
            .WithEndpoint("http", e => e.Transport = "http2")
            .WithExternalHttpEndpoints();

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

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
              ingress: {
                external: true
                targetPort: 8000
                transport: 'http2'
              }
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
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task ProjectUsesTheTargetPortAsADefaultPortForFirstHttpEndpoint()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppsInfrastructure();

        builder.AddProject<Project>("api", launchProfileName: null)
               .WithHttpEndpoint()
               .WithHttpsEndpoint();

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        await ExecuteBeforeStartHooksAsync(app, default);

        var project = Assert.Single(model.GetProjectResources());

        project.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

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
                      name: 'HTTPS_PORTS'
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
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task NonTcpHttpOrUdpSchemeThrows()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppsInfrastructure();

        builder.AddContainer("api", "myimage")
            .WithEndpoint(scheme: "foo");

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => ExecuteBeforeStartHooksAsync(app, default));

        Assert.Equal("The endpoint(s) 'foo' specify an unsupported scheme. The supported schemes are 'http', 'https', and 'tcp'.", ex.Message);
    }

    [Fact]
    public async Task MultipleExternalEndpointsAreNotSupported()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppsInfrastructure();

        builder.AddContainer("api", "myimage")
            .WithHttpEndpoint(name: "ep1")
            .WithHttpEndpoint(name: "ep2")
            .WithExternalHttpEndpoints();

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => ExecuteBeforeStartHooksAsync(app, default));

        Assert.Equal("Multiple external endpoints are not supported", ex.Message);
    }

    [Fact]
    public async Task ExternalNonHttpEndpointsAreNotSupported()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppsInfrastructure();

        builder.AddContainer("api", "myimage")
            .WithEndpoint("ep1", e => e.IsExternal = true);

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => ExecuteBeforeStartHooksAsync(app, default));

        Assert.Equal("External non-HTTP(s) endpoints are not supported", ex.Message);
    }

    [Fact]
    public async Task HttpAndTcpEndpointsCannotHaveTheSameTargetPort()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppsInfrastructure();

        builder.AddContainer("api", "myimage")
            .WithHttpEndpoint(targetPort: 80)
            .WithEndpoint(targetPort: 80);

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => ExecuteBeforeStartHooksAsync(app, default));

        Assert.Equal("HTTP(s) and TCP endpoints cannot be mixed", ex.Message);
    }

    [Fact]
    public async Task DefaultHttpIngressMustUsePort80()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppsInfrastructure();

        builder.AddContainer("api", "myimage")
            .WithHttpEndpoint(port: 8081);

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => ExecuteBeforeStartHooksAsync(app, default));

        Assert.Equal($"The endpoint 'http' is an http endpoint and must use port 80", ex.Message);
    }

    [Fact]
    public async Task DefaultHttpsIngressMustUsePort443()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppsInfrastructure();

        builder.AddContainer("api", "myimage")
            .WithHttpsEndpoint(port: 8081);

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => ExecuteBeforeStartHooksAsync(app, default));

        Assert.Equal($"The endpoint 'https' is an https endpoint and must use port 443", ex.Message);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ExecuteBeforeStartHooksAsync")]
    private static extern Task ExecuteBeforeStartHooksAsync(DistributedApplication app, CancellationToken cancellationToken);

    private sealed class Project : IProjectMetadata
    {
        public string ProjectPath => "project";
    }
}
