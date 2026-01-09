// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.AIFoundry;
using Azure.Provisioning;
using Azure.Provisioning.ApplicationInsights;
using Azure.Provisioning.CognitiveServices;
using Azure.Provisioning.ContainerRegistry;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.Resources;
using Azure.Provisioning.Roles;

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

        void configureInfrastructure(AzureResourceInfrastructure infra)
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

            UserAssignedIdentity identity;
            if (aspireResource.TryGetAppIdentityResource(out var idResource) && idResource is AzureUserAssignedIdentityResource identityResource)
            {
                identity = (UserAssignedIdentity)identityResource.AddAsExistingResource(infra);
            }
            else
            {
                // This is the principal used for the app runtime
                identity = new UserAssignedIdentity(Infrastructure.NormalizeBicepIdentifier($"{prefix}-mi"))
                {
                    Tags = tags
                };
                infra.Add(identity);
            }

            // Use a user-provided container registry or create a new one.
            // The container registry is used to host images for hosted agents.
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

            var pullRa = containerRegistry.CreateRoleAssignment(ContainerRegistryBuiltInRole.AcrPull, identity);
            // There's a bug in the CDK, see https://github.com/Azure/azure-sdk-for-net/issues/47265
            pullRa.Name = BicepFunction.CreateGuid(containerRegistry.Id, identity.Id, pullRa.RoleDefinitionId);
            infra.Add(pullRa);
            infra.Add(containerRegistry);

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
            infra.Add(new ProvisioningOutput("APPLICATION_INSIGHTS_CONNECTION_STRING", typeof(string))
            {
                Value = appInsights.ConnectionString
            });

            // Permissions for publishing telemetry to App Insights
            var pubRoleRa = appInsights.CreateRoleAssignment(ApplicationInsightsBuiltInRole.MonitoringMetricsPublisher, identity);
            pubRoleRa.Name = BicepFunction.CreateGuid(appInsights.Id, identity.Id, pubRoleRa.RoleDefinitionId);
            infra.Add(pubRoleRa);

            var account = builder.Resource.AddAsExistingResource(infra);
            // Create the Cognitive Services project resource
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
                        Name = name,
                        Identity = new ManagedServiceIdentity()
                        {
                            ManagedServiceIdentityType = ManagedServiceIdentityType.UserAssigned,
                            // We hack in this dictionary because the CDK doesn't take BicepValues as
                            // keys.
                            UserAssignedIdentities =
                            {
                                { identity.Id.Compile().ToString(), new UserAssignedIdentityDetails() }
                            }
                        },
                        Properties = new CognitiveServicesProjectProperties
                        {
                            DisplayName = name
                        },
                        Tags = { { "aspire-resource-name", infra.AspireResource.Name } }
                    };
                    return resource;
                });
            infra.Add(new ProvisioningOutput("id", typeof(string))
            {
                Value = project.Id
            });
            infra.Add(new ProvisioningOutput("name", typeof(string)) { Value = project.Name });
            infra.Add(new ProvisioningOutput("endpoint", typeof(string))
            {
                Value = (BicepValue<string>)new IndexExpression((BicepExpression)project.Properties.Endpoints!, "AI Foundry API")
            });
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
                Value = identity.Id
            });
            infra.Add(new ProvisioningOutput("AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_CLIENT_ID", typeof(string))
            {
                Value = identity.ClientId
            });
            // Create an Application Insights connection for the project to use for telemetry
            infra.Add(new CognitiveServicesProjectConnection($"{aspireResource.GetBicepIdentifier()}_ai_conn")
            {
                Parent = project,
                Name = $"{name}-ai-conn",
                Properties = new AppInsightsConnectionProperties()
                {
                    Target = appInsights.Id,
                    IsSharedToAll = false,
                    CredentialsKey = appInsights.ConnectionString,
                    Metadata =
                    {
                        { "ApiType", "Azure" },
                        { "ResourceId", appInsights.Id }
                    }
                }
            });
        }
        var resource = new AzureCognitiveServicesProjectResource(name, configureInfrastructure, builder.Resource);
        return builder.ApplicationBuilder.AddResource(resource);
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
}
