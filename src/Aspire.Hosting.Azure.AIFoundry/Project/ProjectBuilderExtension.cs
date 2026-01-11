// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.AIFoundry;
using Azure.Provisioning;
using Azure.Provisioning.ApplicationInsights;
using Azure.Provisioning.Authorization;
using Azure.Provisioning.CognitiveServices;
using Azure.Provisioning.ContainerRegistry;
using Azure.Provisioning.CosmosDB;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.KeyVault;
using Azure.Provisioning.Primitives;
using Azure.Provisioning.Resources;
using Azure.Provisioning.Roles;
using Azure.Provisioning.Search;
using Azure.Provisioning.Storage;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding Azure Cognitive Services project resources to the distributed application model.
/// </summary>
public static class AzureCognitiveServicesProjectExtensions
{
    /// <summary>
    /// Adds an Azure Cognitive Services project resource to the application model.
    ///
    /// This will also attach the project as a deployment target for agents.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> for the parent Azure Cognitive Services account resource.</param>
    /// <param name="name">The name of the Azure Cognitive Services project resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for the Azure Cognitive Services project resource.</returns>
    public static IResourceBuilder<AzureCognitiveServicesProjectResource> AddProject(
        this IResourceBuilder<AzureAIFoundryResource> builder,
        string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        return builder.ApplicationBuilder.AddResource(new AzureCognitiveServicesProjectResource(name, ConfigureInfrastructure, builder.Resource));
    }

    /// <summary>
    /// Adds an Azure Cognitive Services project resource to the application model.
    ///
    /// This will create a default foundry account resource.
    /// This will also set the project as a deployment target for agents.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> for the parent Azure Cognitive Services account resource.</param>
    /// <param name="name">The name of the Azure Cognitive Services project resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for the Azure Cognitive Services project resource.</returns>
    public static IResourceBuilder<AzureCognitiveServicesProjectResource> AddFoundryProject(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var account = builder.AddAzureAIFoundry($"{name}-foundry");
        return account.AddProject(name);
    }

    /// <summary>
    /// Associates a container registry with the Azure Cognitive Services project resource for
    /// publishing and locating hosted agents.
    /// </summary>
    public static IResourceBuilder<AzureCognitiveServicesProjectResource> WithContainerRegistry(
        this IResourceBuilder<AzureCognitiveServicesProjectResource> builder,
        IResourceBuilder<AzureContainerRegistryResource> registryBuilder)
    {
        return builder.WithContainerRegistry(registryBuilder.Resource);
    }

    /// <summary>
    /// Associates a container registry with the Azure Cognitive Services project resource for
    /// publishing and locating hosted agents.
    /// </summary>
    public static IResourceBuilder<AzureCognitiveServicesProjectResource> WithContainerRegistry(
        this IResourceBuilder<AzureCognitiveServicesProjectResource> builder,
        IContainerRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(registry);

        // This will be queried during the "publish" phase
        builder.Resource.Annotations.Add(new ContainerRegistryReferenceAnnotation(registry));
        return builder;
    }

    /// <summary>
    /// Adds a reference to an Azure Cognitive Services project resource to the destination resource.
    ///
    /// This adds both the standard environment variables (e.g. `ConnectionStrings__{name}={url}`) but also
    /// the `AZURE_AI_PROJECT_ENDPOINT` environment variable.
    /// </summary>
    public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<AzureCognitiveServicesProjectResource> project)
        where TDestination : IResourceWithEnvironment
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(project);

        // Add standard references and environment variables
        ResourceBuilderExtensions.WithReference(builder, project);

        var resource = project.Resource;

        // Determine what to inject based on the annotation on the destination resource
        var injectionAnnotation = builder.Resource.TryGetLastAnnotation<ReferenceEnvironmentInjectionAnnotation>(out var annotation) ? annotation : null;
        var flags = injectionAnnotation?.Flags ?? ReferenceEnvironmentInjectionFlags.All;

        if (flags.HasFlag(ReferenceEnvironmentInjectionFlags.ConnectionString))
        {
            // Also inject the striaght URL as another env var, because the APIProjectClient
            // does not accept a connection string format.
            builder.WithEnvironment("AZURE_AI_PROJECT_ENDPOINT", resource.Endpoint);
            builder.WithEnvironment("APPLICATIONINSIGHTS_CONNECTION_STRING", resource.AppInsightsConnectionString);
        }
        if (builder is IResourceBuilder<IResourceWithWaitSupport> waitableBuilder)
        {
            waitableBuilder.WaitFor(project);
        }
        return builder;
    }

    /// <summary>
    /// Adds an Application Insights connection to the Azure Cognitive Services project
    /// that overrides the default (which is to create a new Application Insights resource).
    /// </summary>
    /// <returns></returns>
    public static IResourceBuilder<AzureBicepResource> WithAppInsights(
        this IResourceBuilder<AzureCognitiveServicesProjectResource> builder,
        IResourceBuilder<AzureApplicationInsightsResource> appInsights)
    {
        builder.Resource.AppInsights = appInsights.Resource;
        return builder;
    }

    /// <summary>
    /// Adds a model deployment to the parent foundry of the Azure Cognitive Services project.
    /// </summary>
    /// <param name="builder">Aspire resource builder for a project</param>
    /// <param name="name">Name to give the model deployment</param>
    /// <param name="model">The <see cref="AIFoundryModel"/> to deploy</param>
    /// <returns></returns>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddModelDeployment(
        this IResourceBuilder<AzureCognitiveServicesProjectResource> builder,
        [ResourceName] string name,
        AIFoundryModel model)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        return builder.ApplicationBuilder.CreateResourceBuilder(builder.Resource.Parent).AddDeployment(name, model);
    }

    /// <summary>
    /// Adds a model deployment to the parent foundry of the Azure Cognitive Services project.
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddModelDeployment(
        this IResourceBuilder<AzureCognitiveServicesProjectResource> builder,
        [ResourceName] string name,
        string modelName,
        string modelVersion,
        string format)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        return builder.ApplicationBuilder.CreateResourceBuilder(builder.Resource.Parent).AddDeployment(name, modelName, modelVersion, format);
    }

    internal static void ConfigureInfrastructure(AzureResourceInfrastructure infra)
    {
        var prefix = infra.AspireResource.Name;
        var aspireResource = (AzureCognitiveServicesProjectResource)infra.AspireResource;
        var tags = new ProvisioningParameter("tags", typeof(object))
        {
            Value = new BicepDictionary<string>()
        };
        infra.Add(tags);

        // This tells azd to avoid creating infrastructure
        var userPrincipalId = new ProvisioningParameter(AzureBicepResource.KnownParameters.UserPrincipalId, typeof(string)) { Value = new BicepValue<string>(string.Empty) };
        infra.Add(userPrincipalId);

        /*
         * Create managed identity
         */

        ManagedServiceIdentity managedIdentity;
        if (aspireResource.TryGetAppIdentityResource(out var idResource) && idResource is AzureUserAssignedIdentityResource identityResource)
        {
            managedIdentity = new ManagedServiceIdentity()
            {
                ManagedServiceIdentityType = ManagedServiceIdentityType.UserAssigned,
                // We hack in this dictionary because the CDK doesn't take BicepValues as
                // keys.
                UserAssignedIdentities =
                {
                    { ((UserAssignedIdentity)identityResource.AddAsExistingResource(infra)).Id.Compile().ToString(), new UserAssignedIdentityDetails() }
                }
            };
        }
        else
        {
            managedIdentity = new ManagedServiceIdentity()
            {
                ManagedServiceIdentityType = ManagedServiceIdentityType.SystemAssigned
            };
        }

        var account = aspireResource.Parent.AddAsExistingResource(infra);

        /*
         * Create the project
         */

        var project = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(
            infra,
            (identifier, resourceName) =>
            {
                var resource = aspireResource.FromExisting(identifier);
                resource.Parent = account;
                resource.Name = resourceName;
                return resource;
            },
            infra =>
            {
                var resource = new CognitiveServicesProject(infra.AspireResource.GetBicepIdentifier())
                {
                    Parent = account,
                    Name = aspireResource.Name,
                    Identity = managedIdentity,
                    Properties = new CognitiveServicesProjectProperties
                    {
                        DisplayName = aspireResource.Name
                    },
                    Tags = { { "aspire-resource-name", infra.AspireResource.Name } }
                };
                return resource;
            });
        var projectPrincipalId = project.Identity.PrincipalId;
        infra.Add(new ProvisioningOutput("id", typeof(string))
        {
            Value = project.Id
        });
        infra.Add(new ProvisioningOutput("name", typeof(string)) { Value = project.Name });
        infra.Add(new ProvisioningOutput("endpoint", typeof(string))
        {
            Value = (BicepValue<string>)new IndexExpression((BicepExpression)project.Properties.Endpoints!, "AI Foundry API")
        });
        infra.Add(new ProvisioningOutput("principalId", typeof(string))
        {
            Value = projectPrincipalId
        });

        /*
         * Container registry for hosted agents
         */

        ContainerRegistryService? containerRegistry = null;
        if (aspireResource.TryGetLastAnnotation<ContainerRegistryReferenceAnnotation>(out var registryReferenceAnnotation) && registryReferenceAnnotation.Registry is AzureProvisioningResource registry)
        {
            containerRegistry = (ContainerRegistryService)registry.AddAsExistingResource(infra);
        }
        else
        {
            containerRegistry = new ContainerRegistryService(Infrastructure.NormalizeBicepIdentifier($"{prefix}_acr"))
            {
                Sku = new() { Name = ContainerRegistrySkuName.Basic },
                Tags = tags
            };
        }
        // Project needs this to pull hosted agent images and run them
        var pullRa = containerRegistry.CreateRoleAssignment(ContainerRegistryBuiltInRole.AcrPull, RoleManagementPrincipalType.ServicePrincipal, projectPrincipalId);
        // There's a bug in the CDK, see https://github.com/Azure/azure-sdk-for-net/issues/47265
        pullRa.Name = BicepFunction.CreateGuid(containerRegistry.Id, project.Id, pullRa.RoleDefinitionId);
        infra.Add(pullRa);
        infra.Add(containerRegistry);
        infra.Add(new ProvisioningOutput("AZURE_CONTAINER_REGISTRY_ENDPOINT", typeof(string))
        {
            Value = containerRegistry.LoginServer
        });
        infra.Add(new ProvisioningOutput("AZURE_CONTAINER_REGISTRY_NAME", typeof(string))
        {
            Value = containerRegistry.Name
        });
        infra.Add(new ProvisioningOutput("AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID", typeof(string))
        {
            Value = projectPrincipalId
        });

        // Implicit dependencies for all other connections
        List<ProvisionableResource> allConnDeps = [];
        // Implicit dependencies for capability hosts
        List<ProvisionableResource> capHostDeps = [];

        /*
         * Set up key vault access if applicable
         */
        if (aspireResource.KeyVault is not null)
        {
            var keyVault = (KeyVaultService)aspireResource.KeyVault.AddAsExistingResource(infra);
            var kvRa = keyVault.CreateRoleAssignment(KeyVaultBuiltInRole.KeyVaultSecretsUser, RoleManagementPrincipalType.ServicePrincipal, projectPrincipalId);
            kvRa.Name = BicepFunction.CreateGuid(keyVault.Id, project.Id, kvRa.RoleDefinitionId);
            infra.Add(kvRa);
            infra.Add(new CognitiveServicesProjectConnection($"{aspireResource.GetBicepIdentifier()}_kv_conn")
            {
                Parent = project,
                Name = $"{project.Name}-kv-conn",
                Properties = new AzureKeyVaultConnectionProperties()
                {
                    Target = keyVault.Id,
                    IsSharedToAll = false
                }
            });
            allConnDeps.Add(keyVault);
            allConnDeps.Add(kvRa);
        }

        /*
         * Application Insights for telemetry
         */

        ApplicationInsightsComponent appInsights;
        if (aspireResource.AppInsights is not null && !aspireResource.AppInsights.IsEmulator())
        {
            appInsights = (ApplicationInsightsComponent)aspireResource.AppInsights.AddAsExistingResource(infra);
        }
        else
        {
            appInsights = new ApplicationInsightsComponent(Infrastructure.NormalizeBicepIdentifier($"{prefix}-ai"))
            {
                ApplicationType = ApplicationInsightsApplicationType.Web,
                Kind = "web",
                Tags = tags
            };
            infra.Add(appInsights);
        }
        var pubRoleRa = appInsights.CreateRoleAssignment(ApplicationInsightsBuiltInRole.MonitoringMetricsPublisher, RoleManagementPrincipalType.ServicePrincipal, projectPrincipalId);
        pubRoleRa.Name = BicepFunction.CreateGuid(appInsights.Id, project.Id, pubRoleRa.RoleDefinitionId);
        infra.Add(pubRoleRa);
        // This is for passing into hosted agent application code
        infra.Add(new ProvisioningOutput("APPLICATION_INSIGHTS_CONNECTION_STRING", typeof(string))
        {
            Value = appInsights.ConnectionString
        });
        // Project needs a connection to send server-side telemetry
        var appInsightsConn = new CognitiveServicesProjectConnection($"{aspireResource.GetBicepIdentifier()}_ai_conn")
        {
            Parent = project,
            Name = $"{project.Name}-ai-conn",
            Properties = new AppInsightsConnectionProperties()
            {
                Target = appInsights.Id,
                IsSharedToAll = false,
                CredentialsKey = appInsights.ConnectionString,
                Metadata =
                {
                    { "ApiType", "Azure" },
                    { "ResourceId", appInsights.Id },
                    { "location", appInsights.Location }
                }
            }
        };
        foreach (var dep in allConnDeps)
        {
            appInsightsConn.DependsOn.Add(dep);
        }
        infra.Add(appInsightsConn);

        /*
         * Storage
         */

        if (aspireResource.Storage is not null)
        {
            var storage = (StorageAccount)aspireResource.Storage.AddAsExistingResource(infra);
            var storageConn = new CognitiveServicesProjectConnection($"{aspireResource.GetBicepIdentifier()}_storage_conn")
            {
                Parent = project,
                Name = BicepFunction.Interpolate($"{project.Name}-{storage.Name}"),
                Properties = new AzureStorageAccountConnectionProperties()
                {
                    Target = aspireResource.Storage.BlobEndpoint.AsProvisioningParameter(infra),
                    Metadata =
                    {
                        { "ApiType", "Azure" },
                        { "ResourceId", storage.Id },
                        { "location", storage.Location }
                    }
                }
            };
            infra.Add(storageConn);
            foreach (var dep in allConnDeps)
            {
                storageConn.DependsOn.Add(dep);
            }
            var storageRoleRa = storage.CreateRoleAssignment(StorageBuiltInRole.StorageBlobDataContributor, RoleManagementPrincipalType.ServicePrincipal, projectPrincipalId);
            storageRoleRa.Name = BicepFunction.CreateGuid(storage.Id, project.Id, storageRoleRa.RoleDefinitionId);
            infra.Add(storageRoleRa);
            capHostDeps.Add(storage);
            capHostDeps.Add(storageRoleRa);
        }

        /*
         * CosmosDB
         */

        if (aspireResource.CosmosDB is not null)
        {
            var cosmosDb = (CosmosDBAccount)aspireResource.CosmosDB.AddAsExistingResource(infra);
            var cosmosDbConn = new CognitiveServicesProjectConnection($"{aspireResource.GetBicepIdentifier()}_cosmosdb_conn")
            {
                Parent = project,
                Name = BicepFunction.Interpolate($"{project.Name}-{cosmosDb.Name}"),
                Properties = new AadAuthTypeConnectionProperties()
                {
                    Category = CognitiveServicesConnectionCategory.CosmosDB,
                    // This is the document endpoint
                    Target = aspireResource.CosmosDB.ConnectionStringOutput.AsProvisioningParameter(infra),
                    Metadata =
                    {
                        { "ApiType", "Azure" },
                        { "ResourceId", cosmosDb.Id },
                        { "location", cosmosDb.Location }
                    }
                }
            };
            infra.Add(cosmosDbConn);
            foreach (var dep in allConnDeps)
            {
                cosmosDbConn.DependsOn.Add(dep);
            }
            var cosmosDbRoleRa = cosmosDb.CreateRoleAssignment(CosmosDBBuiltInRole.CosmosDBOperator, RoleManagementPrincipalType.ServicePrincipal, projectPrincipalId);
            cosmosDbRoleRa.Name = BicepFunction.CreateGuid(cosmosDb.Id, project.Id, cosmosDbRoleRa.RoleDefinitionId);
            infra.Add(cosmosDbRoleRa);
            // TODO: add role 00000000-0000-0000-0000-000000000002 (Cosmos DB Built-in Data Contributor) for data plane access
            // like this:
            // var roleDefinitionId = resourceId(
            //   'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions',
            //   cosmosAccountName,
            //   '00000000-0000-0000-0000-000000000002'
            // )
            // var accountScope = '/subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.DocumentDB/databaseAccounts/${cosmosAccountName}'
            // resource containerRoleAssignmentUserContainer 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2022-05-15' = {
            //   parent: cosmosAccount
            //   name: guid(projectWorkspaceId, cosmosAccountName, roleDefinitionId, projectPrincipalId)
            //   properties: {
            //       principalId: projectPrincipalId
            //       roleDefinitionId: roleDefinitionId
            //       scope: accountScope
            //   }
            // }
            capHostDeps.Add(cosmosDb);
            capHostDeps.Add(cosmosDbRoleRa);
        }

        /*
         * Azure Search
         */

        if (aspireResource.SearchService is not null)
        {
            var searchService = (SearchService)aspireResource.SearchService.AddAsExistingResource(infra);
            var searchConn = new CognitiveServicesProjectConnection($"{aspireResource.GetBicepIdentifier()}_search_conn")
            {
                Parent = project,
                Name = BicepFunction.Interpolate($"{project.Name}-{searchService.Name}"),
                Properties = new AadAuthTypeConnectionProperties()
                {
                    Category = CognitiveServicesConnectionCategory.CognitiveSearch,
                    Target = BicepFunction.Interpolate($"https://{searchService.Name}.search.windows.net"),
                    Metadata =
                    {
                        { "ApiType", "Azure" },
                        { "ResourceId", searchService.Id },
                        { "location", searchService.Location }
                    }
                }
            };
            infra.Add(searchConn);
            foreach (var dep in allConnDeps)
            {
                searchConn.DependsOn.Add(dep);
            }
            var contributor = searchService.CreateRoleAssignment(SearchBuiltInRole.SearchServiceContributor, RoleManagementPrincipalType.ServicePrincipal, projectPrincipalId);
            contributor.Name = BicepFunction.CreateGuid(searchService.Id, project.Id, contributor.RoleDefinitionId);
            infra.Add(contributor);
            var indexDataContrib = searchService.CreateRoleAssignment(SearchBuiltInRole.SearchIndexDataContributor, RoleManagementPrincipalType.ServicePrincipal, projectPrincipalId);
            indexDataContrib.Name = BicepFunction.CreateGuid(searchService.Id, project.Id, indexDataContrib.RoleDefinitionId);
            infra.Add(indexDataContrib);
            capHostDeps.Add(searchService);
            capHostDeps.Add(contributor);
            capHostDeps.Add(indexDataContrib);
        }

        // TODO: provision capability host that use these dependencies
    }
}
