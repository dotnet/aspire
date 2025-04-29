// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREACADOMAINS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.AppContainers;
using Aspire.Hosting.Utils;
using Azure.Provisioning;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.KeyVault;
using Azure.Provisioning.Primitives;
using Azure.Provisioning.Storage;
using Microsoft.Extensions.DependencyInjection;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureContainerAppsTests(ITestOutputHelper output)
{
    [Fact]
    public async Task AddContainerAppsInfrastructureAddsDeploymentTargetWithContainerAppToContainerResources()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

#pragma warning disable CS0618 // Type or member is obsolete
        builder.AddAzureContainerAppsInfrastructure();
#pragma warning restore CS0618 // Type or member is obsolete

        builder.AddContainer("api", "myimage");

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

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
            "outputs_azure_container_apps_environment_default_domain": "{.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}",
            "outputs_azure_container_apps_environment_id": "{.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID}"
          }
        }
        """;

        Assert.Equal(expectedManifest, m);

        var expectedBicep =
        """
        @description('The location for the resource(s) to be deployed.')
        param location string = resourceGroup().location
        
        param outputs_azure_container_apps_environment_default_domain string

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
                }
              ]
              scale: {
                minReplicas: 1
              }
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

        builder.AddAzureContainerAppEnvironment("env");

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

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        var m = manifest.ToString();

        var expectedManifest =
        """
        {
          "type": "azure.bicep.v0",
          "path": "api.module.bicep",
          "params": {
            "env_outputs_azure_container_apps_environment_default_domain": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}",
            "env_outputs_azure_container_apps_environment_id": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID}",
            "env_outputs_azure_container_registry_endpoint": "{env.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT}",
            "env_outputs_azure_container_registry_managed_identity_id": "{env.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID}",
            "api_containerimage": "{api.containerImage}"
          }
        }
        """;

        Assert.Equal(expectedManifest, m);

        var expectedBicep =
        """
        @description('The location for the resource(s) to be deployed.')
        param location string = resourceGroup().location
        
        param env_outputs_azure_container_apps_environment_default_domain string

        param env_outputs_azure_container_apps_environment_id string
        
        param env_outputs_azure_container_registry_endpoint string
        
        param env_outputs_azure_container_registry_managed_identity_id string
        
        param api_containerimage string
        
        resource api 'Microsoft.App/containerApps@2024-03-01' = {
          name: 'api'
          location: location
          properties: {
            configuration: {
              activeRevisionsMode: 'Single'
              registries: [
                {
                  server: env_outputs_azure_container_registry_endpoint
                  identity: env_outputs_azure_container_registry_managed_identity_id
                }
              ]
            }
            environmentId: env_outputs_azure_container_apps_environment_id
            template: {
              containers: [
                {
                  image: api_containerimage
                  name: 'api'
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
              '${env_outputs_azure_container_registry_managed_identity_id}': { }
            }
          }
        }
        """;
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task AddContainerAppEnvironmentAddsDeploymentTargetWithContainerAppToProjectResources()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var env = builder.AddAzureContainerAppEnvironment("env");

        builder.AddProject<Project>("api", launchProfileName: null)
            .WithHttpEndpoint();

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetProjectResources());

        var target = container.GetDeploymentTargetAnnotation();

        Assert.Same(env.Resource, target?.ComputeEnvironment);

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
            "api_containerport": "{api.containerPort}",
            "env_outputs_azure_container_apps_environment_default_domain": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}",
            "env_outputs_azure_container_apps_environment_id": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID}",
            "env_outputs_azure_container_registry_endpoint": "{env.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT}",
            "env_outputs_azure_container_registry_managed_identity_id": "{env.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID}",
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

        param env_outputs_azure_container_apps_environment_default_domain string

        param env_outputs_azure_container_apps_environment_id string

        param env_outputs_azure_container_registry_endpoint string

        param env_outputs_azure_container_registry_managed_identity_id string

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
                  server: env_outputs_azure_container_registry_endpoint
                  identity: env_outputs_azure_container_registry_managed_identity_id
                }
              ]
            }
            environmentId: env_outputs_azure_container_apps_environment_id
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
              '${env_outputs_azure_container_registry_managed_identity_id}': { }
            }
          }
        }
        """;
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task AddExecutableResourceWithPublishAsDockerFileWithAppsInfrastructureAddsDeploymentTargetWithContainerAppToContainerResources()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var infra = builder.AddAzureContainerAppEnvironment("infra");

        var env = builder.AddParameter("env");

        builder.AddExecutable("api", "node.exe", Environment.CurrentDirectory)
               .PublishAsDockerFile()
               .PublishAsAzureContainerApp((infra, app) =>
               {
                   app.Template.Containers[0].Value!.Env.Add(new ContainerAppEnvironmentVariable()
                   {
                       Name = "Hello",
                       Value = env.AsProvisioningParameter(infra)
                   });
               });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        var target = container.GetDeploymentTargetAnnotation();

        Assert.Same(infra.Resource, target?.ComputeEnvironment);

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
            "infra_outputs_azure_container_apps_environment_default_domain": "{infra.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}",
            "infra_outputs_azure_container_apps_environment_id": "{infra.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID}",
            "infra_outputs_azure_container_registry_endpoint": "{infra.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT}",
            "infra_outputs_azure_container_registry_managed_identity_id": "{infra.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID}",
            "api_containerimage": "{api.containerImage}",
            "env": "{env.value}"
          }
        }
        """;

        Assert.Equal(expectedManifest, m);

        var expectedBicep =
        """
        @description('The location for the resource(s) to be deployed.')
        param location string = resourceGroup().location
        
        param infra_outputs_azure_container_apps_environment_default_domain string

        param infra_outputs_azure_container_apps_environment_id string
        
        param infra_outputs_azure_container_registry_endpoint string
        
        param infra_outputs_azure_container_registry_managed_identity_id string
        
        param api_containerimage string
        
        param env string
        
        resource api 'Microsoft.App/containerApps@2024-03-01' = {
          name: 'api'
          location: location
          properties: {
            configuration: {
              activeRevisionsMode: 'Single'
              registries: [
                {
                  server: infra_outputs_azure_container_registry_endpoint
                  identity: infra_outputs_azure_container_registry_managed_identity_id
                }
              ]
            }
            environmentId: infra_outputs_azure_container_apps_environment_id
            template: {
              containers: [
                {
                  image: api_containerimage
                  name: 'api'
                  env: [
                    {
                      name: 'Hello'
                      value: env
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
              '${infra_outputs_azure_container_registry_managed_identity_id}': { }
            }
          }
        }
        """;
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task CanTweakContainerAppEnvironmentUsingPublishAsContainerAppOnExecutable()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var env = builder.AddAzureContainerAppEnvironment("env");

        builder.AddExecutable("api", "node.exe", Environment.CurrentDirectory)
               .PublishAsDockerFile();

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        var target = container.GetDeploymentTargetAnnotation();

        Assert.Same(env.Resource, target?.ComputeEnvironment);

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
            "env_outputs_azure_container_apps_environment_default_domain": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}",
            "env_outputs_azure_container_apps_environment_id": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID}",
            "env_outputs_azure_container_registry_endpoint": "{env.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT}",
            "env_outputs_azure_container_registry_managed_identity_id": "{env.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID}",
            "api_containerimage": "{api.containerImage}"
          }
        }
        """;

        Assert.Equal(expectedManifest, m);

        var expectedBicep =
        """
        @description('The location for the resource(s) to be deployed.')
        param location string = resourceGroup().location
        
        param env_outputs_azure_container_apps_environment_default_domain string

        param env_outputs_azure_container_apps_environment_id string
        
        param env_outputs_azure_container_registry_endpoint string
        
        param env_outputs_azure_container_registry_managed_identity_id string
        
        param api_containerimage string
        
        resource api 'Microsoft.App/containerApps@2024-03-01' = {
          name: 'api'
          location: location
          properties: {
            configuration: {
              activeRevisionsMode: 'Single'
              registries: [
                {
                  server: env_outputs_azure_container_registry_endpoint
                  identity: env_outputs_azure_container_registry_managed_identity_id
                }
              ]
            }
            environmentId: env_outputs_azure_container_apps_environment_id
            template: {
              containers: [
                {
                  image: api_containerimage
                  name: 'api'
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
              '${env_outputs_azure_container_registry_managed_identity_id}': { }
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

        builder.AddAzureContainerAppEnvironment("env");

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

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        var m = manifest.ToString();

        var expectedManifest =
        """
        {
          "type": "azure.bicep.v0",
          "path": "api.module.bicep",
          "params": {
            "env_outputs_azure_container_apps_environment_default_domain": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}",
            "env_outputs_azure_container_apps_environment_id": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID}",
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
        
        param env_outputs_azure_container_apps_environment_default_domain string

        param env_outputs_azure_container_apps_environment_id string
        
        param value string
        
        param minReplicas string
        
        resource api 'Microsoft.App/containerApps@2024-03-01' = {
          name: 'api'
          location: location
          properties: {
            configuration: {
              activeRevisionsMode: 'Single'
            }
            environmentId: env_outputs_azure_container_apps_environment_id
            template: {
              containers: [
                {
                  image: 'myimage:latest'
                  name: 'api'
                  env: [
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
        }
        """;

        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task AddContainerAppsEntrypointAndArgs()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        builder.AddContainer("api", "myimage")
               .WithEntrypoint("/bin/sh")
               .WithArgs("my", "args with space");

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        var expectedBicep =
        """
        @description('The location for the resource(s) to be deployed.')
        param location string = resourceGroup().location
        
        param env_outputs_azure_container_apps_environment_default_domain string

        param env_outputs_azure_container_apps_environment_id string
        
        resource api 'Microsoft.App/containerApps@2024-03-01' = {
          name: 'api'
          location: location
          properties: {
            configuration: {
              activeRevisionsMode: 'Single'
            }
            environmentId: env_outputs_azure_container_apps_environment_id
            template: {
              containers: [
                {
                  image: 'myimage:latest'
                  name: 'api'
                  command: [
                    '/bin/sh'
                  ]
                  args: [
                    'my'
                    'args with space'
                  ]
                }
              ]
              scale: {
                minReplicas: 1
              }
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

        builder.AddAzureContainerAppEnvironment("env");

        var db = builder.AddAzureCosmosDB("mydb");
        db.AddCosmosDatabase("cosmosdb", databaseName: "db");

        // Postgres uses secret outputs + a literal connection string
        var pgdb = builder.AddAzurePostgresFlexibleServer("pg").WithPasswordAuthentication().AddDatabase("db");

        var rawCs = builder.AddConnectionString("cs");

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
            .WithEnvironment("Value", value)
            .WithEnvironment("CS", rawCs);

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
            context.EnvironmentVariables["HOSTANDPORT"] = httpEp.Property(EndpointProperty.HostAndPort);
            context.EnvironmentVariables["SCHEME"] = httpEp.Property(EndpointProperty.Scheme);
            context.EnvironmentVariables["INTERNAL_HOSTANDPORT"] = internalEp.Property(EndpointProperty.HostAndPort);
        });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var proj = Assert.Single(model.GetProjectResources());
        var identityName = $"{proj.Name}-identity";
        var projIdentity = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == identityName);

        proj.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);
        var (identityManifest, identityBicep) = await GetManifestWithBicep(projIdentity);

        var m = manifest.ToString();

        var expectedManifest =
        """
        {
          "type": "azure.bicep.v0",
          "path": "api.module.bicep",
          "params": {
            "api_identity_outputs_id": "{api-identity.outputs.id}",
            "api_identity_outputs_clientid": "{api-identity.outputs.clientId}",
            "api_containerport": "{api.containerPort}",
            "mydb_outputs_connectionstring": "{mydb.outputs.connectionString}",
            "storage_outputs_blobendpoint": "{storage.outputs.blobEndpoint}",
            "pg_kv_outputs_name": "{pg-kv.outputs.name}",
            "value0_value": "{value0.value}",
            "value1_value": "{value1.value}",
            "cs_connectionstring": "{cs.connectionString}",
            "env_outputs_azure_container_apps_environment_default_domain": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}",
            "env_outputs_azure_container_apps_environment_id": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID}",
            "env_outputs_azure_container_registry_endpoint": "{env.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT}",
            "env_outputs_azure_container_registry_managed_identity_id": "{env.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID}",
            "api_containerimage": "{api.containerImage}"
          }
        }
        """;

        Assert.Equal(expectedManifest, m);

        var expectedIdentityManifest =
        """
        {
          "type": "azure.bicep.v0",
          "path": "api-identity.module.bicep"
        }
        """;

        Assert.Equal(expectedIdentityManifest, identityManifest.ToString());

        var expectedBicep =
        """
        @description('The location for the resource(s) to be deployed.')
        param location string = resourceGroup().location
        
        param api_identity_outputs_id string
        
        param api_identity_outputs_clientid string
        
        param api_containerport string
        
        param mydb_outputs_connectionstring string
        
        param storage_outputs_blobendpoint string
        
        param pg_kv_outputs_name string
        
        @secure()
        param value0_value string
        
        param value1_value string
        
        @secure()
        param cs_connectionstring string
        
        param env_outputs_azure_container_apps_environment_default_domain string
        
        param env_outputs_azure_container_apps_environment_id string
        
        param env_outputs_azure_container_registry_endpoint string
        
        param env_outputs_azure_container_registry_managed_identity_id string
        
        param api_containerimage string
        
        resource pg_kv_outputs_name_kv 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
          name: pg_kv_outputs_name
        }
        
        resource pg_kv_outputs_name_kv_connectionstrings__db 'Microsoft.KeyVault/vaults/secrets@2023-07-01' existing = {
          name: 'connectionstrings--db'
          parent: pg_kv_outputs_name_kv
        }
        
        resource api 'Microsoft.App/containerApps@2024-03-01' = {
          name: 'api'
          location: location
          properties: {
            configuration: {
              secrets: [
                {
                  name: 'connectionstrings--db'
                  identity: api_identity_outputs_id
                  keyVaultUrl: pg_kv_outputs_name_kv_connectionstrings__db.properties.secretUri
                }
                {
                  name: 'secretval'
                  value: value0_value
                }
                {
                  name: 'secret-value-1'
                  value: value0_value
                }
                {
                  name: 'cs'
                  value: cs_connectionstring
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
                  server: env_outputs_azure_container_registry_endpoint
                  identity: env_outputs_azure_container_registry_managed_identity_id
                }
              ]
            }
            environmentId: env_outputs_azure_container_apps_environment_id
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
                      value: mydb_outputs_connectionstring
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
                      name: 'CS'
                      secretRef: 'cs'
                    }
                    {
                      name: 'HTTP_EP'
                      value: 'http://api.internal.${env_outputs_azure_container_apps_environment_default_domain}'
                    }
                    {
                      name: 'HTTPS_EP'
                      value: 'https://api.internal.${env_outputs_azure_container_apps_environment_default_domain}'
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
                      value: 'api.internal.${env_outputs_azure_container_apps_environment_default_domain}'
                    }
                    {
                      name: 'HOSTANDPORT'
                      value: 'api.internal.${env_outputs_azure_container_apps_environment_default_domain}:80'
                    }
                    {
                      name: 'SCHEME'
                      value: 'http'
                    }
                    {
                      name: 'INTERNAL_HOSTANDPORT'
                      value: 'api:8000'
                    }
                    {
                      name: 'AZURE_CLIENT_ID'
                      value: api_identity_outputs_clientid
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
              '${api_identity_outputs_id}': { }
              '${env_outputs_azure_container_registry_managed_identity_id}': { }
            }
          }
        }
        """;
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);

        var expectedIdentityBicep =
        """
        @description('The location for the resource(s) to be deployed.')
        param location string = resourceGroup().location

        resource api_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
          name: take('api_identity-${uniqueString(resourceGroup().id)}', 128)
          location: location
        }

        output id string = api_identity.id
        
        output clientId string = api_identity.properties.clientId
        
        output principalId string = api_identity.properties.principalId

        output principalName string = api_identity.name
        """;

        output.WriteLine(identityBicep);
        Assert.Equal(expectedIdentityBicep, identityBicep);
    }

    [Fact]
    public async Task ProjectWithManyReferenceTypesAndContainerAppEnvironment()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("cae");

        var db = builder.AddAzureCosmosDB("mydb");
        db.AddCosmosDatabase("cosmosdb", databaseName: "db");

        // Postgres uses secret outputs + a literal connection string
        var pgdb = builder.AddAzurePostgresFlexibleServer("pg").WithPasswordAuthentication().AddDatabase("db");

        var rawCs = builder.AddConnectionString("cs");

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
            .WithEnvironment("Value", value)
            .WithEnvironment("CS", rawCs);

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
            context.EnvironmentVariables["HOSTANDPORT"] = httpEp.Property(EndpointProperty.HostAndPort);
            context.EnvironmentVariables["SCHEME"] = httpEp.Property(EndpointProperty.Scheme);
            context.EnvironmentVariables["INTERNAL_HOSTANDPORT"] = internalEp.Property(EndpointProperty.HostAndPort);
        });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var proj = Assert.Single(model.GetProjectResources());
        var identityName = $"{proj.Name}-identity";
        var projIdentity = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == identityName);

        proj.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);
        var (identityManifest, identityBicep) = await GetManifestWithBicep(projIdentity);

        var m = manifest.ToString();

        var expectedManifest =
        """
        {
          "type": "azure.bicep.v0",
          "path": "api.module.bicep",
          "params": {
            "api_identity_outputs_id": "{api-identity.outputs.id}",
            "api_identity_outputs_clientid": "{api-identity.outputs.clientId}",
            "api_containerport": "{api.containerPort}",
            "mydb_outputs_connectionstring": "{mydb.outputs.connectionString}",
            "storage_outputs_blobendpoint": "{storage.outputs.blobEndpoint}",
            "pg_kv_outputs_name": "{pg-kv.outputs.name}",
            "value0_value": "{value0.value}",
            "value1_value": "{value1.value}",
            "cs_connectionstring": "{cs.connectionString}",
            "cae_outputs_azure_container_apps_environment_default_domain": "{cae.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}",
            "cae_outputs_azure_container_apps_environment_id": "{cae.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID}",
            "cae_outputs_azure_container_registry_endpoint": "{cae.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT}",
            "cae_outputs_azure_container_registry_managed_identity_id": "{cae.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID}",
            "api_containerimage": "{api.containerImage}"
          }
        }
        """;

        Assert.Equal(expectedManifest, m);

        var expectedIdentityManifest =
        """
        {
          "type": "azure.bicep.v0",
          "path": "api-identity.module.bicep"
        }
        """;

        Assert.Equal(expectedIdentityManifest, identityManifest.ToString());
    }

    [Fact]
    public void MultipleCallsToAddAzureContainerAppEnvironmentThrows()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env1");
        var ex = Assert.Throws<NotSupportedException>(() => builder.AddAzureContainerAppEnvironment("env2"));

        Assert.Equal("Only one container app environment is supported at this time. Found: env1", ex.Message);
    }

    [Fact]
    public async Task MultipleAzureContainerAppEnvironmentThrows()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env1");

        builder.Resources.Add(new AzureContainerAppEnvironmentResource("env2", infra => { }));

        using var app = builder.Build();

        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => ExecuteBeforeStartHooksAsync(app, default));

        Assert.Equal("Multiple container app environments are not supported.", ex.Message);
    }

    [Fact]
    public async Task PublishAsContainerAppInfluencesContainerAppDefinition()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");
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

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        var m = manifest.ToString();

        var expectedManifest =
        """
        {
          "type": "azure.bicep.v0",
          "path": "api.module.bicep",
          "params": {
            "env_outputs_azure_container_apps_environment_default_domain": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}",
            "env_outputs_azure_container_apps_environment_id": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID}"
          }
        }
        """;

        Assert.Equal(expectedManifest, m);

        var expectedBicep =
        """
        @description('The location for the resource(s) to be deployed.')
        param location string = resourceGroup().location
        
        param env_outputs_azure_container_apps_environment_default_domain string

        param env_outputs_azure_container_apps_environment_id string
        
        resource api 'Microsoft.App/containerApps@2024-03-01' = {
          name: 'api'
          location: location
          properties: {
            configuration: {
              activeRevisionsMode: 'Single'
            }
            environmentId: env_outputs_azure_container_apps_environment_id
            template: {
              containers: [
                {
                  image: 'myimage:latest'
                  name: 'api'
                }
              ]
              scale: {
                minReplicas: 0
              }
            }
          }
        }
        """;
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task ConfigureCustomDomainMutatesIngress()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var customDomain = builder.AddParameter("customDomain");
        var certificateName = builder.AddParameter("certificateName");

        builder.AddAzureContainerAppEnvironment("env");
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

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        var m = manifest.ToString();

        var expectedManifest =
        """
        {
          "type": "azure.bicep.v0",
          "path": "api.module.bicep",
          "params": {
            "env_outputs_azure_container_apps_environment_default_domain": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}",
            "env_outputs_azure_container_apps_environment_id": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID}",
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
        
        param env_outputs_azure_container_apps_environment_default_domain string

        param env_outputs_azure_container_apps_environment_id string
        
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
                    certificateId: (certificateName != '') ? '${env_outputs_azure_container_apps_environment_id}/managedCertificates/${certificateName}' : null
                  }
                ]
              }
            }
            environmentId: env_outputs_azure_container_apps_environment_id
            template: {
              containers: [
                {
                  image: 'myimage:latest'
                  name: 'api'
                }
              ]
              scale: {
                minReplicas: 1
              }
            }
          }
        }
        """;
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task ConfigureDuplicateCustomDomainMutatesIngress()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var customDomain = builder.AddParameter("customDomain");
        var initialCertificateName = builder.AddParameter("initialCertificateName");
        var expectedCertificateName = builder.AddParameter("expectedCertificateName");

        builder.AddAzureContainerAppEnvironment("env");
        builder.AddContainer("api", "myimage")
            .WithHttpEndpoint(targetPort: 1111)
            .PublishAsAzureContainerApp((module, c) =>
            {
                c.ConfigureCustomDomain(customDomain, initialCertificateName);
                c.ConfigureCustomDomain(customDomain, expectedCertificateName);
            });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureBicepResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        var m = manifest.ToString();

        var expectedManifest =
        """
        {
          "type": "azure.bicep.v0",
          "path": "api.module.bicep",
          "params": {
            "env_outputs_azure_container_apps_environment_default_domain": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}",
            "env_outputs_azure_container_apps_environment_id": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID}",
            "initialCertificateName": "{initialCertificateName.value}",
            "customDomain": "{customDomain.value}",
            "expectedCertificateName": "{expectedCertificateName.value}"
          }
        }
        """;

        Assert.Equal(expectedManifest, m);

        var expectedBicep =
        """
        @description('The location for the resource(s) to be deployed.')
        param location string = resourceGroup().location
        
        param env_outputs_azure_container_apps_environment_default_domain string

        param env_outputs_azure_container_apps_environment_id string
        
        param initialCertificateName string
        
        param customDomain string
        
        param expectedCertificateName string
        
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
                    bindingType: (expectedCertificateName != '') ? 'SniEnabled' : 'Disabled'
                    certificateId: (expectedCertificateName != '') ? '${env_outputs_azure_container_apps_environment_id}/managedCertificates/${expectedCertificateName}' : null
                  }
                ]
              }
            }
            environmentId: env_outputs_azure_container_apps_environment_id
            template: {
              containers: [
                {
                  image: 'myimage:latest'
                  name: 'api'
                }
              ]
              scale: {
                minReplicas: 1
              }
            }
          }
        }
        """;
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task ConfigureMultipleCustomDomainsMutatesIngress()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var customDomain1 = builder.AddParameter("customDomain1");
        var certificateName1 = builder.AddParameter("certificateName1");

        var customDomain2 = builder.AddParameter("customDomain2");
        var certificateName2 = builder.AddParameter("certificateName2");

        builder.AddAzureContainerAppEnvironment("env");
        builder.AddContainer("api", "myimage")
            .WithHttpEndpoint(targetPort: 1111)
            .PublishAsAzureContainerApp((module, c) =>
            {
                c.ConfigureCustomDomain(customDomain1, certificateName1);
                c.ConfigureCustomDomain(customDomain2, certificateName2);
            });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureBicepResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        var m = manifest.ToString();

        var expectedManifest =
        """
        {
          "type": "azure.bicep.v0",
          "path": "api.module.bicep",
          "params": {
            "env_outputs_azure_container_apps_environment_default_domain": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}",
            "env_outputs_azure_container_apps_environment_id": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID}",
            "certificateName1": "{certificateName1.value}",
            "customDomain1": "{customDomain1.value}",
            "certificateName2": "{certificateName2.value}",
            "customDomain2": "{customDomain2.value}"
          }
        }
        """;

        Assert.Equal(expectedManifest, m);

        var expectedBicep =
        """
        @description('The location for the resource(s) to be deployed.')
        param location string = resourceGroup().location
        
        param env_outputs_azure_container_apps_environment_default_domain string

        param env_outputs_azure_container_apps_environment_id string
        
        param certificateName1 string
        
        param customDomain1 string
        
        param certificateName2 string
        
        param customDomain2 string
        
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
                    name: customDomain1
                    bindingType: (certificateName1 != '') ? 'SniEnabled' : 'Disabled'
                    certificateId: (certificateName1 != '') ? '${env_outputs_azure_container_apps_environment_id}/managedCertificates/${certificateName1}' : null
                  }
                  {
                    name: customDomain2
                    bindingType: (certificateName2 != '') ? 'SniEnabled' : 'Disabled'
                    certificateId: (certificateName2 != '') ? '${env_outputs_azure_container_apps_environment_id}/managedCertificates/${certificateName2}' : null
                  }
                ]
              }
            }
            environmentId: env_outputs_azure_container_apps_environment_id
            template: {
              containers: [
                {
                  image: 'myimage:latest'
                  name: 'api'
                }
              ]
              scale: {
                minReplicas: 1
              }
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

        builder.AddAzureContainerAppEnvironment("env");

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

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        var m = manifest.ToString();

        var expectedManifest =
        """
        {
          "type": "azure.bicep.v0",
          "path": "api.module.bicep",
          "params": {
            "env_outputs_volumes_api_0": "{env.outputs.volumes_api_0}",
            "env_outputs_volumes_api_1": "{env.outputs.volumes_api_1}",
            "env_outputs_bindmounts_api_0": "{env.outputs.bindmounts_api_0}",
            "env_outputs_azure_container_apps_environment_default_domain": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}",
            "env_outputs_azure_container_apps_environment_id": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID}"
          }
        }
        """;

        Assert.Equal(expectedManifest, m);

        var expectedBicep =
        """
        @description('The location for the resource(s) to be deployed.')
        param location string = resourceGroup().location

        param env_outputs_volumes_api_0 string

        param env_outputs_volumes_api_1 string

        param env_outputs_bindmounts_api_0 string

        param env_outputs_azure_container_apps_environment_default_domain string

        param env_outputs_azure_container_apps_environment_id string

        resource api 'Microsoft.App/containerApps@2024-03-01' = {
          name: 'api'
          location: location
          properties: {
            configuration: {
              activeRevisionsMode: 'Single'
            }
            environmentId: env_outputs_azure_container_apps_environment_id
            template: {
              containers: [
                {
                  image: 'myimage:latest'
                  name: 'api'
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
                  storageName: env_outputs_volumes_api_0
                }
                {
                  name: 'v1'
                  storageType: 'AzureFile'
                  storageName: env_outputs_volumes_api_1
                }
                {
                  name: 'bm0'
                  storageType: 'AzureFile'
                  storageName: env_outputs_bindmounts_api_0
                }
              ]
            }
          }
        }
        """;
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task SecretOutputHandling()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

#pragma warning disable CS0618 // Type or member is obsolete
        builder.AddAzureContainerAppsInfrastructure();
#pragma warning restore CS0618 // Type or member is obsolete

        var db = builder.AddAzureCosmosDB("mydb").WithAccessKeyAuthentication();
        db.AddCosmosDatabase("db");

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

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        var m = manifest.ToString();

        var expectedManifest =
        """
        {
          "type": "azure.bicep.v0",
          "path": "api.module.bicep",
          "params": {
            "api_identity_outputs_id": "{api-identity.outputs.id}",
            "api_identity_outputs_clientid": "{api-identity.outputs.clientId}",
            "mydb_kv_outputs_name": "{mydb-kv.outputs.name}",
            "mydb_secretoutputs": "{mydb.secretOutputs}",
            "mydb_secretoutputs_connectionstring": "{mydb.secretOutputs.connectionString}",
            "mydb_secretoutputs_connectionstring1": "{mydb.secretOutputs.connectionString1}",
            "outputs_azure_container_apps_environment_default_domain": "{.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}",
            "outputs_azure_container_apps_environment_id": "{.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID}"
          }
        }
        """;

        Assert.Equal(expectedManifest, m);

        var expectedBicep =
        """
        @description('The location for the resource(s) to be deployed.')
        param location string = resourceGroup().location
        
        param api_identity_outputs_id string
        
        param api_identity_outputs_clientid string
        
        param mydb_kv_outputs_name string
        
        param mydb_secretoutputs string
        
        @secure()
        param mydb_secretoutputs_connectionstring string
        
        @secure()
        param mydb_secretoutputs_connectionstring1 string
        
        param outputs_azure_container_apps_environment_default_domain string

        param outputs_azure_container_apps_environment_id string
        
        resource mydb_kv_outputs_name_kv 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
          name: mydb_kv_outputs_name
        }
        
        resource mydb_secretoutputs_kv 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
          name: mydb_secretoutputs
        }
        
        resource mydb_kv_outputs_name_kv_connectionstrings__mydb 'Microsoft.KeyVault/vaults/secrets@2023-07-01' existing = {
          name: 'connectionstrings--mydb'
          parent: mydb_kv_outputs_name_kv
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
                  identity: api_identity_outputs_id
                  keyVaultUrl: mydb_kv_outputs_name_kv_connectionstrings__mydb.properties.secretUri
                }
                {
                  name: 'connectionstring'
                  identity: api_identity_outputs_id
                  keyVaultUrl: mydb_secretoutputs_kv_connectionString.properties.secretUri
                }
                {
                  name: 'secret0'
                  identity: api_identity_outputs_id
                  keyVaultUrl: mydb_secretoutputs_kv_connectionString.properties.secretUri
                }
                {
                  name: 'secret1'
                  identity: api_identity_outputs_id
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
                      value: api_identity_outputs_clientid
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
              '${api_identity_outputs_id}': { }
            }
          }
        }
        """;
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task SecretOutputsThrowNotSupportedExceptionWithContainerAppEnvironmentResource()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("cae");

        var resource = builder.AddAzureInfrastructure("resourceWithSecret", infra =>
        {
            var kvNameParam = new ProvisioningParameter(AzureBicepResource.KnownParameters.KeyVaultName, typeof(string));
            infra.Add(kvNameParam);

            var kv = KeyVaultService.FromExisting("kv");
            kv.Name = kvNameParam;
            infra.Add(kv);

            var secret = new KeyVaultSecret("kvs")
            {
                Name = "myconnection",
                Properties = new()
                {
                    Value = "top secret"
                },
                Parent = kv,
            };

            infra.Add(secret);
        });

        builder.AddContainer("api", "image")
            .WithEnvironment(context =>
            {
                context.EnvironmentVariables["secret0"] = resource.GetSecretOutput("myconnection");
            });

        using var app = builder.Build();

        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => ExecuteBeforeStartHooksAsync(app, default));

        Assert.Equal("Automatic Key vault generation is not supported in this environment. Please create a key vault resource directly.", ex.Message);
    }

    [Fact]
    public async Task CanCustomizeWithProvisioningBuildOptions()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.Services.Configure<AzureProvisioningOptions>(options => options.ProvisioningBuildOptions.InfrastructureResolvers.Insert(0, new MyResourceNamePropertyResolver()));
        builder.AddAzureContainerAppEnvironment("env");

        builder.AddContainer("api1", "myimage");

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var container = Assert.Single(model.GetContainerResources());

        container.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (_, bicep) = await GetManifestWithBicep(resource);

        var expectedBicep =
        """
        @description('The location for the resource(s) to be deployed.')
        param location string = resourceGroup().location
        
        param env_outputs_azure_container_apps_environment_default_domain string

        param env_outputs_azure_container_apps_environment_id string
        
        resource api1 'Microsoft.App/containerApps@2024-03-01' = {
          name: 'api1-my'
          location: location
          properties: {
            configuration: {
              activeRevisionsMode: 'Single'
            }
            environmentId: env_outputs_azure_container_apps_environment_id
            template: {
              containers: [
                {
                  image: 'myimage:latest'
                  name: 'api1'
                }
              ]
              scale: {
                minReplicas: 1
              }
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

        builder.AddAzureContainerAppEnvironment("env");

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

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        var m = manifest.ToString();

        var expectedManifest =
        """
        {
          "type": "azure.bicep.v0",
          "path": "api.module.bicep",
          "params": {
            "env_outputs_azure_container_apps_environment_default_domain": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}",
            "env_outputs_azure_container_apps_environment_id": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID}"
          }
        }
        """;

        Assert.Equal(expectedManifest, m);

        var expectedBicep =
        """
        @description('The location for the resource(s) to be deployed.')
        param location string = resourceGroup().location
        
        param env_outputs_azure_container_apps_environment_default_domain string

        param env_outputs_azure_container_apps_environment_id string
        
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
            environmentId: env_outputs_azure_container_apps_environment_id
            template: {
              containers: [
                {
                  image: 'myimage:latest'
                  name: 'api'
                }
              ]
              scale: {
                minReplicas: 1
              }
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

        builder.AddAzureContainerAppEnvironment("env");

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

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        var m = manifest.ToString();

        var expectedManifest =
        """
        {
          "type": "azure.bicep.v0",
          "path": "api.module.bicep",
          "params": {
            "env_outputs_azure_container_apps_environment_default_domain": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}",
            "env_outputs_azure_container_apps_environment_id": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID}"
          }
        }
        """;

        Assert.Equal(expectedManifest, m);

        var expectedBicep =
        """
        @description('The location for the resource(s) to be deployed.')
        param location string = resourceGroup().location
        
        param env_outputs_azure_container_apps_environment_default_domain string

        param env_outputs_azure_container_apps_environment_id string
        
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
            environmentId: env_outputs_azure_container_apps_environment_id
            template: {
              containers: [
                {
                  image: 'myimage:latest'
                  name: 'api'
                }
              ]
              scale: {
                minReplicas: 1
              }
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

        builder.AddAzureContainerAppEnvironment("env");

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

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        var m = manifest.ToString();

        var expectedManifest =
        """
        {
          "type": "azure.bicep.v0",
          "path": "api.module.bicep",
          "params": {
            "env_outputs_azure_container_apps_environment_default_domain": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}",
            "env_outputs_azure_container_apps_environment_id": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID}"
          }
        }
        """;

        Assert.Equal(expectedManifest, m);

        var expectedBicep =
        """
        @description('The location for the resource(s) to be deployed.')
        param location string = resourceGroup().location
        
        param env_outputs_azure_container_apps_environment_default_domain string

        param env_outputs_azure_container_apps_environment_id string
        
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
            environmentId: env_outputs_azure_container_apps_environment_id
            template: {
              containers: [
                {
                  image: 'myimage:latest'
                  name: 'api'
                }
              ]
              scale: {
                minReplicas: 1
              }
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

        builder.AddAzureContainerAppEnvironment("env");

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

        var (manifest, bicep) = await GetManifestWithBicep(resource);

        var m = manifest.ToString();

        var expectedManifest =
        """
        {
          "type": "azure.bicep.v0",
          "path": "api.module.bicep",
          "params": {
            "api_containerport": "{api.containerPort}",
            "env_outputs_azure_container_apps_environment_default_domain": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}",
            "env_outputs_azure_container_apps_environment_id": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID}",
            "env_outputs_azure_container_registry_endpoint": "{env.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT}",
            "env_outputs_azure_container_registry_managed_identity_id": "{env.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID}",
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
        
        param env_outputs_azure_container_apps_environment_default_domain string

        param env_outputs_azure_container_apps_environment_id string
        
        param env_outputs_azure_container_registry_endpoint string
        
        param env_outputs_azure_container_registry_managed_identity_id string
        
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
                  server: env_outputs_azure_container_registry_endpoint
                  identity: env_outputs_azure_container_registry_managed_identity_id
                }
              ]
            }
            environmentId: env_outputs_azure_container_apps_environment_id
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
              '${env_outputs_azure_container_registry_managed_identity_id}': { }
            }
          }
        }
        """;
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    [Fact]
    public async Task RoleAssignmentsWithAsExisting()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        var storageName = builder.AddParameter("storageName");
        var storageRG = builder.AddParameter("storageRG");

        var storage = builder.AddAzureStorage("storage")
            .PublishAsExisting(storageName, storageRG);
        var blobs = storage.AddBlobs("blobs");

        builder.AddProject<Project>("api", launchProfileName: null)
               .WithRoleAssignments(storage, StorageBuiltInRole.StorageBlobDataReader);

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        await ExecuteBeforeStartHooksAsync(app, default);

        var project = Assert.Single(model.GetProjectResources());
        var projIdentity = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "api-identity");
        var projRolesStorage = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "api-roles-storage");

        project.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);
        var (identityManifest, identityBicep) = await GetManifestWithBicep(projIdentity);
        var (rolesStorageManifest, rolesStorageBicep) = await GetManifestWithBicep(projRolesStorage);

        var expectedManifest =
            """
            {
              "type": "azure.bicep.v0",
              "path": "api.module.bicep",
              "params": {
                "api_identity_outputs_id": "{api-identity.outputs.id}",
                "api_identity_outputs_clientid": "{api-identity.outputs.clientId}",
                "env_outputs_azure_container_apps_environment_default_domain": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}",
                "env_outputs_azure_container_apps_environment_id": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID}",
                "env_outputs_azure_container_registry_endpoint": "{env.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT}",
                "env_outputs_azure_container_registry_managed_identity_id": "{env.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID}",
                "api_containerimage": "{api.containerImage}"
              }
            }
            """;
        var m = manifest.ToString();
        output.WriteLine(m);
        Assert.Equal(expectedManifest, m);

        var expectedIdentityManifest =
            """
            {
              "type": "azure.bicep.v0",
              "path": "api-identity.module.bicep"
            }
            """;
        Assert.Equal(expectedIdentityManifest, identityManifest.ToString());

        var expectedRolesStorageManifest =
            """
            {
              "type": "azure.bicep.v1",
              "path": "api-roles-storage.module.bicep",
              "params": {
                "storage_outputs_name": "{storage.outputs.name}",
                "principalId": "{api-identity.outputs.principalId}"
              },
              "scope": {
                "resourceGroup": "{storageRG.value}"
              }
            }
            """;
        Assert.Equal(expectedRolesStorageManifest, rolesStorageManifest.ToString());

        var expectedBicep =
            """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location
            
            param api_identity_outputs_id string
            
            param api_identity_outputs_clientid string
            
            param env_outputs_azure_container_apps_environment_default_domain string

            param env_outputs_azure_container_apps_environment_id string
            
            param env_outputs_azure_container_registry_endpoint string
            
            param env_outputs_azure_container_registry_managed_identity_id string
            
            param api_containerimage string
            
            resource api 'Microsoft.App/containerApps@2024-03-01' = {
              name: 'api'
              location: location
              properties: {
                configuration: {
                  activeRevisionsMode: 'Single'
                  registries: [
                    {
                      server: env_outputs_azure_container_registry_endpoint
                      identity: env_outputs_azure_container_registry_managed_identity_id
                    }
                  ]
                }
                environmentId: env_outputs_azure_container_apps_environment_id
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
                          name: 'AZURE_CLIENT_ID'
                          value: api_identity_outputs_clientid
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
                  '${api_identity_outputs_id}': { }
                  '${env_outputs_azure_container_registry_managed_identity_id}': { }
                }
              }
            }
            """;
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);

        var expectedIdentityBicep =
            """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            resource api_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
              name: take('api_identity-${uniqueString(resourceGroup().id)}', 128)
              location: location
            }

            output id string = api_identity.id

            output clientId string = api_identity.properties.clientId

            output principalId string = api_identity.properties.principalId

            output principalName string = api_identity.name
            """;
        output.WriteLine(identityBicep);
        Assert.Equal(expectedIdentityBicep, identityBicep);

        var expectedRolesStorageBicep =
            """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param storage_outputs_name string

            param principalId string

            resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' existing = {
              name: storage_outputs_name
            }

            resource storage_StorageBlobDataReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(storage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '2a2b9908-6ea1-4ae2-8e65-a410df84e7d1'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '2a2b9908-6ea1-4ae2-8e65-a410df84e7d1')
                principalType: 'ServicePrincipal'
              }
              scope: storage
            }
            """;
        output.WriteLine(rolesStorageBicep);
        Assert.Equal(expectedRolesStorageBicep, rolesStorageBicep);
    }

    [Fact]
    public async Task RoleAssignmentsWithAsExistingCosmosDB()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        var cosmosName = builder.AddParameter("cosmosName");
        var cosmosRG = builder.AddParameter("cosmosRG");

        var cosmos = builder.AddAzureCosmosDB("cosmos")
            .PublishAsExisting(cosmosName, cosmosRG);

        builder.AddProject<Project>("api", launchProfileName: null)
               .WithReference(cosmos);

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        await ExecuteBeforeStartHooksAsync(app, default);

        var project = Assert.Single(model.GetProjectResources());
        var projIdentity = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "api-identity");
        var projRolesStorage = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "api-roles-cosmos");

        project.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);
        var (identityManifest, identityBicep) = await GetManifestWithBicep(projIdentity);
        var (rolesCosmosManifest, rolesCosmosBicep) = await GetManifestWithBicep(projRolesStorage);

        var expectedManifest =
            """
            {
              "type": "azure.bicep.v0",
              "path": "api.module.bicep",
              "params": {
                "api_identity_outputs_id": "{api-identity.outputs.id}",
                "api_identity_outputs_clientid": "{api-identity.outputs.clientId}",
                "cosmos_outputs_connectionstring": "{cosmos.outputs.connectionString}",
                "env_outputs_azure_container_apps_environment_default_domain": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}",
                "env_outputs_azure_container_apps_environment_id": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID}",
                "env_outputs_azure_container_registry_endpoint": "{env.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT}",
                "env_outputs_azure_container_registry_managed_identity_id": "{env.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID}",
                "api_containerimage": "{api.containerImage}"
              }
            }
            """;
        var m = manifest.ToString();
        output.WriteLine(m);
        Assert.Equal(expectedManifest, m);

        var expectedIdentityManifest =
            """
            {
              "type": "azure.bicep.v0",
              "path": "api-identity.module.bicep"
            }
            """;
        Assert.Equal(expectedIdentityManifest, identityManifest.ToString());

        var expectedRolesCosmosManifest =
            """
            {
              "type": "azure.bicep.v1",
              "path": "api-roles-cosmos.module.bicep",
              "params": {
                "cosmos_outputs_name": "{cosmos.outputs.name}",
                "principalId": "{api-identity.outputs.principalId}"
              },
              "scope": {
                "resourceGroup": "{cosmosRG.value}"
              }
            }
            """;
        Assert.Equal(expectedRolesCosmosManifest, rolesCosmosManifest.ToString());

        var expectedBicep =
            """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location
            
            param api_identity_outputs_id string
            
            param api_identity_outputs_clientid string
            
            param cosmos_outputs_connectionstring string
            
            param env_outputs_azure_container_apps_environment_default_domain string

            param env_outputs_azure_container_apps_environment_id string
            
            param env_outputs_azure_container_registry_endpoint string
            
            param env_outputs_azure_container_registry_managed_identity_id string
            
            param api_containerimage string
            
            resource api 'Microsoft.App/containerApps@2024-03-01' = {
              name: 'api'
              location: location
              properties: {
                configuration: {
                  activeRevisionsMode: 'Single'
                  registries: [
                    {
                      server: env_outputs_azure_container_registry_endpoint
                      identity: env_outputs_azure_container_registry_managed_identity_id
                    }
                  ]
                }
                environmentId: env_outputs_azure_container_apps_environment_id
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
                          name: 'ConnectionStrings__cosmos'
                          value: cosmos_outputs_connectionstring
                        }
                        {
                          name: 'AZURE_CLIENT_ID'
                          value: api_identity_outputs_clientid
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
                  '${api_identity_outputs_id}': { }
                  '${env_outputs_azure_container_registry_managed_identity_id}': { }
                }
              }
            }
            """;
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);

        var expectedIdentityBicep =
            """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            resource api_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
              name: take('api_identity-${uniqueString(resourceGroup().id)}', 128)
              location: location
            }

            output id string = api_identity.id

            output clientId string = api_identity.properties.clientId

            output principalId string = api_identity.properties.principalId

            output principalName string = api_identity.name
            """;
        output.WriteLine(identityBicep);
        Assert.Equal(expectedIdentityBicep, identityBicep);

        var expectedRolesCosmosBicep =
            """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param cosmos_outputs_name string

            param principalId string

            resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' existing = {
              name: cosmos_outputs_name
            }

            resource cosmos_roleDefinition 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2024-08-15' existing = {
              name: '00000000-0000-0000-0000-000000000002'
              parent: cosmos
            }

            resource cosmos_roleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-08-15' = {
              name: guid(principalId, cosmos_roleDefinition.id, cosmos.id)
              properties: {
                principalId: principalId
                roleDefinitionId: cosmos_roleDefinition.id
                scope: cosmos.id
              }
              parent: cosmos
            }
            """;
        output.WriteLine(rolesCosmosBicep);
        Assert.Equal(expectedRolesCosmosBicep, rolesCosmosBicep);
    }

    [Fact]
    public async Task RoleAssignmentsWithAsExistingRedis()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        var redis = builder.AddAzureRedis("redis")
            .PublishAsExisting("myredis", "myRG");

        builder.AddProject<Project>("api", launchProfileName: null)
            .WithReference(redis);

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        await ExecuteBeforeStartHooksAsync(app, default);

        var project = Assert.Single(model.GetProjectResources());
        var projIdentity = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "api-identity");
        var projRolesStorage = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "api-roles-redis");

        project.TryGetLastAnnotation<DeploymentTargetAnnotation>(out var target);

        var resource = target?.DeploymentTarget as AzureProvisioningResource;

        Assert.NotNull(resource);

        var (manifest, bicep) = await GetManifestWithBicep(resource);
        var (identityManifest, identityBicep) = await GetManifestWithBicep(projIdentity);
        var (rolesRedisManifest, rolesRedisBicep) = await GetManifestWithBicep(projRolesStorage);

        var expectedManifest =
            """
            {
              "type": "azure.bicep.v0",
              "path": "api.module.bicep",
              "params": {
                "api_identity_outputs_id": "{api-identity.outputs.id}",
                "api_identity_outputs_clientid": "{api-identity.outputs.clientId}",
                "redis_outputs_connectionstring": "{redis.outputs.connectionString}",
                "env_outputs_azure_container_apps_environment_default_domain": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}",
                "env_outputs_azure_container_apps_environment_id": "{env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID}",
                "env_outputs_azure_container_registry_endpoint": "{env.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT}",
                "env_outputs_azure_container_registry_managed_identity_id": "{env.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID}",
                "api_containerimage": "{api.containerImage}"
              }
            }
            """;
        var m = manifest.ToString();
        output.WriteLine(m);
        Assert.Equal(expectedManifest, m);

        var expectedIdentityManifest =
            """
            {
              "type": "azure.bicep.v0",
              "path": "api-identity.module.bicep"
            }
            """;
        Assert.Equal(expectedIdentityManifest, identityManifest.ToString());

        var expectedRolesRedisManifest =
            """
            {
              "type": "azure.bicep.v1",
              "path": "api-roles-redis.module.bicep",
              "params": {
                "redis_outputs_name": "{redis.outputs.name}",
                "principalId": "{api-identity.outputs.principalId}",
                "principalName": "{api-identity.outputs.principalName}"
              },
              "scope": {
                "resourceGroup": "myRG"
              }
            }
            """;
        Assert.Equal(expectedRolesRedisManifest, rolesRedisManifest.ToString());

        var expectedBicep =
            """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location
            
            param api_identity_outputs_id string
            
            param api_identity_outputs_clientid string
            
            param redis_outputs_connectionstring string
            
            param env_outputs_azure_container_apps_environment_default_domain string

            param env_outputs_azure_container_apps_environment_id string
            
            param env_outputs_azure_container_registry_endpoint string
            
            param env_outputs_azure_container_registry_managed_identity_id string
            
            param api_containerimage string
            
            resource api 'Microsoft.App/containerApps@2024-03-01' = {
              name: 'api'
              location: location
              properties: {
                configuration: {
                  activeRevisionsMode: 'Single'
                  registries: [
                    {
                      server: env_outputs_azure_container_registry_endpoint
                      identity: env_outputs_azure_container_registry_managed_identity_id
                    }
                  ]
                }
                environmentId: env_outputs_azure_container_apps_environment_id
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
                          name: 'ConnectionStrings__redis'
                          value: redis_outputs_connectionstring
                        }
                        {
                          name: 'AZURE_CLIENT_ID'
                          value: api_identity_outputs_clientid
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
                  '${api_identity_outputs_id}': { }
                  '${env_outputs_azure_container_registry_managed_identity_id}': { }
                }
              }
            }
            """;
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);

        var expectedIdentityBicep =
            """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            resource api_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
              name: take('api_identity-${uniqueString(resourceGroup().id)}', 128)
              location: location
            }

            output id string = api_identity.id

            output clientId string = api_identity.properties.clientId

            output principalId string = api_identity.properties.principalId

            output principalName string = api_identity.name
            """;
        output.WriteLine(identityBicep);
        Assert.Equal(expectedIdentityBicep, identityBicep);

        var expectedRolesRedisBicep =
            """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param redis_outputs_name string

            param principalId string

            param principalName string

            resource redis 'Microsoft.Cache/redis@2024-03-01' existing = {
              name: redis_outputs_name
            }

            resource redis_contributor 'Microsoft.Cache/redis/accessPolicyAssignments@2024-03-01' = {
              name: guid(redis.id, principalId, 'Data Contributor')
              properties: {
                accessPolicyName: 'Data Contributor'
                objectId: principalId
                objectIdAlias: principalName
              }
              parent: redis
            }
            """;
        output.WriteLine(rolesRedisBicep);
        Assert.Equal(expectedRolesRedisBicep, rolesRedisBicep);
    }

    [Fact]
    public async Task NonTcpHttpOrUdpSchemeThrows()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

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

        builder.AddAzureContainerAppEnvironment("env");

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

        builder.AddAzureContainerAppEnvironment("env");

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

        builder.AddAzureContainerAppEnvironment("env");

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

        builder.AddAzureContainerAppEnvironment("env");

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

        builder.AddAzureContainerAppEnvironment("env");

        builder.AddContainer("api", "myimage")
            .WithHttpsEndpoint(port: 8081);

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => ExecuteBeforeStartHooksAsync(app, default));

        Assert.Equal($"The endpoint 'https' is an https endpoint and must use port 443", ex.Message);
    }

    [Fact]
    public async Task AddContainerAppEnvironmentDoesNotAddEnvironmentResourceInRunMode()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        builder.AddAzureContainerAppEnvironment("env");

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        Assert.Empty(model.Resources.OfType<AzureContainerAppEnvironmentResource>());
    }

    [Fact]
    public async Task KnownParametersAreNotSetWhenUsingAzdResources()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

#pragma warning disable CS0618 // Type or member is obsolete
        builder.AddAzureContainerAppsInfrastructure();
#pragma warning restore CS0618 // Type or member is obsolete

        var pg = builder.AddAzurePostgresFlexibleServer("pg")
                        .WithPasswordAuthentication()
                        .AddDatabase("db");

        builder.AddContainer("cache", "redis")
               .WithVolume("data", "/data")
               .WithReference(pg);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        foreach (var resource in model.Resources.OfType<AzureBicepResource>())
        {
            foreach (var param in resource.Parameters)
            {
                if (param.Key == AzureBicepResource.KnownParameters.KeyVaultName)
                {
                    // Skip kv since we fill it in by default
                    continue;
                }

                if (AzureBicepResource.KnownParameters.IsKnownParameterName(param.Key))
                {
                    Assert.Equal(string.Empty, param.Value);
                }
            }
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddContainerAppEnvironmentAddsEnvironmentResource(bool useAzdNaming)
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var env = builder.AddAzureContainerAppEnvironment("env");

        if (useAzdNaming)
        {
            env.WithAzdResourceNaming();
        }

        var pg = builder.AddAzurePostgresFlexibleServer("pg")
                        .WithPasswordAuthentication()
                        .AddDatabase("db");

        builder.AddContainer("cache", "redis")
               .WithVolume("App.da-ta", "/data")
               .WithReference(pg);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var environment = Assert.Single(model.Resources.OfType<AzureContainerAppEnvironmentResource>());

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

        string expectedBicep;
        if (useAzdNaming)
        {
            expectedBicep =
            """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param userPrincipalId string

            param tags object = { }

            var resourceToken = uniqueString(resourceGroup().id)

            resource env_mi 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
              name: 'mi-${resourceToken}'
              location: location
              tags: tags
            }

            resource env_acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
              name: replace('acr-${resourceToken}', '-', '')
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

            resource env_law 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
              name: 'law-${resourceToken}'
              location: location
              properties: {
                sku: {
                  name: 'PerGB2018'
                }
              }
              tags: tags
            }

            resource env 'Microsoft.App/managedEnvironments@2024-03-01' = {
              name: 'cae-${resourceToken}'
              location: location
              properties: {
                appLogsConfiguration: {
                  destination: 'log-analytics'
                  logAnalyticsConfiguration: {
                    customerId: env_law.properties.customerId
                    sharedKey: env_law.listKeys().primarySharedKey
                  }
                }
                workloadProfiles: [
                  {
                    name: 'consumption'
                    workloadProfileType: 'Consumption'
                  }
                ]
              }
              tags: tags
            }

            resource aspireDashboard 'Microsoft.App/managedEnvironments/dotNetComponents@2024-10-02-preview' = {
              name: 'aspire-dashboard'
              properties: {
                componentType: 'AspireDashboard'
              }
              parent: env
            }

            resource env_Contributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(env.id, userPrincipalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c'))
              properties: {
                principalId: userPrincipalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c')
              }
              scope: env
            }

            resource env_storageVolume 'Microsoft.Storage/storageAccounts@2024-01-01' = {
              name: 'vol${resourceToken}'
              kind: 'StorageV2'
              location: location
              sku: {
                name: 'Standard_LRS'
              }
              properties: {
                largeFileSharesState: 'Enabled'
              }
              tags: tags
            }

            resource storageVolumeFileService 'Microsoft.Storage/storageAccounts/fileServices@2024-01-01' = {
              name: 'default'
              parent: env_storageVolume
            }

            resource shares_volumes_cache_0 'Microsoft.Storage/storageAccounts/fileServices/shares@2024-01-01' = {
              name: take('${toLower('cache')}-${toLower('Appdata')}', 60)
              properties: {
                enabledProtocols: 'SMB'
                shareQuota: 1024
              }
              parent: storageVolumeFileService
            }

            resource managedStorage_volumes_cache_0 'Microsoft.App/managedEnvironments/storages@2024-03-01' = {
              name: take('${toLower('cache')}-${toLower('Appdata')}', 32)
              properties: {
                azureFile: {
                  accountName: env_storageVolume.name
                  accountKey: env_storageVolume.listKeys().keys[0].value
                  accessMode: 'ReadWrite'
                  shareName: shares_volumes_cache_0.name
                }
              }
              parent: env
            }

            output volumes_cache_0 string = managedStorage_volumes_cache_0.name

            output MANAGED_IDENTITY_NAME string = 'mi-${resourceToken}'

            output MANAGED_IDENTITY_PRINCIPAL_ID string = env_mi.properties.principalId

            output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = 'law-${resourceToken}'

            output AZURE_LOG_ANALYTICS_WORKSPACE_ID string = env_law.id

            output AZURE_CONTAINER_REGISTRY_NAME string = replace('acr-${resourceToken}', '-', '')

            output AZURE_CONTAINER_REGISTRY_ENDPOINT string = env_acr.properties.loginServer

            output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = env_mi.id

            output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = 'cae-${resourceToken}'

            output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = env.id

            output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = env.properties.defaultDomain
            """;
        }
        else
        {
            expectedBicep =
            """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param userPrincipalId string

            param tags object = { }

            resource env_mi 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
              name: take('env_mi-${uniqueString(resourceGroup().id)}', 128)
              location: location
              tags: tags
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

            resource env_law 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
              name: take('envlaw-${uniqueString(resourceGroup().id)}', 63)
              location: location
              properties: {
                sku: {
                  name: 'PerGB2018'
                }
              }
              tags: tags
            }

            resource env 'Microsoft.App/managedEnvironments@2024-03-01' = {
              name: take('env${uniqueString(resourceGroup().id)}', 24)
              location: location
              properties: {
                appLogsConfiguration: {
                  destination: 'log-analytics'
                  logAnalyticsConfiguration: {
                    customerId: env_law.properties.customerId
                    sharedKey: env_law.listKeys().primarySharedKey
                  }
                }
                workloadProfiles: [
                  {
                    name: 'consumption'
                    workloadProfileType: 'Consumption'
                  }
                ]
              }
              tags: tags
            }

            resource aspireDashboard 'Microsoft.App/managedEnvironments/dotNetComponents@2024-10-02-preview' = {
              name: 'aspire-dashboard'
              properties: {
                componentType: 'AspireDashboard'
              }
              parent: env
            }

            resource env_Contributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(env.id, userPrincipalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c'))
              properties: {
                principalId: userPrincipalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c')
              }
              scope: env
            }

            resource env_storageVolume 'Microsoft.Storage/storageAccounts@2024-01-01' = {
              name: take('envstoragevolume${uniqueString(resourceGroup().id)}', 24)
              kind: 'StorageV2'
              location: location
              sku: {
                name: 'Standard_LRS'
              }
              properties: {
                largeFileSharesState: 'Enabled'
              }
              tags: tags
            }

            resource storageVolumeFileService 'Microsoft.Storage/storageAccounts/fileServices@2024-01-01' = {
              name: 'default'
              parent: env_storageVolume
            }

            resource shares_volumes_cache_0 'Microsoft.Storage/storageAccounts/fileServices/shares@2024-01-01' = {
              name: take('sharesvolumescache0-${uniqueString(resourceGroup().id)}', 63)
              properties: {
                enabledProtocols: 'SMB'
                shareQuota: 1024
              }
              parent: storageVolumeFileService
            }

            resource managedStorage_volumes_cache_0 'Microsoft.App/managedEnvironments/storages@2024-03-01' = {
              name: take('managedstoragevolumescache${uniqueString(resourceGroup().id)}', 24)
              properties: {
                azureFile: {
                  accountName: env_storageVolume.name
                  accountKey: env_storageVolume.listKeys().keys[0].value
                  accessMode: 'ReadWrite'
                  shareName: shares_volumes_cache_0.name
                }
              }
              parent: env
            }

            output volumes_cache_0 string = managedStorage_volumes_cache_0.name

            output MANAGED_IDENTITY_NAME string = env_mi.name

            output MANAGED_IDENTITY_PRINCIPAL_ID string = env_mi.properties.principalId

            output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = env_law.name

            output AZURE_LOG_ANALYTICS_WORKSPACE_ID string = env_law.id

            output AZURE_CONTAINER_REGISTRY_NAME string = env_acr.name

            output AZURE_CONTAINER_REGISTRY_ENDPOINT string = env_acr.properties.loginServer

            output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = env_mi.id

            output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = env.name

            output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = env.id

            output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = env.properties.defaultDomain
            """;
        }
        output.WriteLine(bicep);
        Assert.Equal(expectedBicep, bicep);
    }

    // see https://github.com/dotnet/aspire/issues/8381 for more information on this scenario
    // Azure SqlServer needs an admin when it is first provisioned. To supply this, we use the
    // principalId from the Azure Container App Environment.
    [Fact]
    public async Task AddContainerAppEnvironmentWorksWithSqlServer()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("env");

        var sql = builder.AddAzureSqlServer("sql");
        var db = sql.AddDatabase("db");

        builder.AddContainer("cache", "redis")
               .WithReference(db);

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var (manifest, bicep) = await GetManifestWithBicep(sql.Resource);

        var m = manifest.ToString();

        var expectedManifest =
        """
        {
          "type": "azure.bicep.v0",
          "connectionString": "Server=tcp:{sql.outputs.sqlServerFqdn},1433;Encrypt=True;Authentication=\u0022Active Directory Default\u0022",
          "path": "sql.module.bicep",
          "params": {
            "principalId": "{env.outputs.MANAGED_IDENTITY_PRINCIPAL_ID}",
            "principalName": "{env.outputs.MANAGED_IDENTITY_NAME}"
          }
        }
        """;
        Assert.Equal(expectedManifest, m);

        var expectedBicep =
        """
        @description('The location for the resource(s) to be deployed.')
        param location string = resourceGroup().location

        param principalId string

        param principalName string

        resource sql 'Microsoft.Sql/servers@2021-11-01' = {
          name: take('sql-${uniqueString(resourceGroup().id)}', 63)
          location: location
          properties: {
            administrators: {
              administratorType: 'ActiveDirectory'
              login: principalName
              sid: principalId
              tenantId: subscription().tenantId
              azureADOnlyAuthentication: true
            }
            minimalTlsVersion: '1.2'
            publicNetworkAccess: 'Enabled'
            version: '12.0'
          }
          tags: {
            'aspire-resource-name': 'sql'
          }
        }

        resource sqlFirewallRule_AllowAllAzureIps 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
          name: 'AllowAllAzureIps'
          properties: {
            endIpAddress: '0.0.0.0'
            startIpAddress: '0.0.0.0'
          }
          parent: sql
        }

        resource db 'Microsoft.Sql/servers/databases@2021-11-01' = {
          name: 'db'
          location: location
          parent: sql
        }

        output sqlServerFqdn string = sql.properties.fullyQualifiedDomainName

        output name string = sql.name
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
