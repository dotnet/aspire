// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.AIFoundry;
using Aspire.Hosting.Eventing;
using Azure.Provisioning;
using Azure.Provisioning.CognitiveServices;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.Resources;
using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using static Azure.Provisioning.Expressions.BicepFunction;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure AI Foundry resources to the application model.
/// </summary>
public static class AzureAIFoundryExtensions
{
    /// <summary>
    /// Adds an Azure OpenAI resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureAIFoundryResource> AddAzureAIFoundry(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        builder.AddAzureProvisioning();

        var resource = new AzureAIFoundryResource(name, ConfigureInfrastructure);
        return builder.AddResource(resource)
            .WithDefaultRoleAssignments(CognitiveServicesBuiltInRole.GetBuiltInRoleName,
                CognitiveServicesBuiltInRole.CognitiveServicesUser, CognitiveServicesBuiltInRole.CognitiveServicesOpenAIUser);
    }

    /// <summary>
    /// Adds and returns an Azure AI Foundry Deployment resource to the application model.
    /// </summary>
    /// <param name="builder">The Azure AI Foundry resource builder.</param>
    /// <param name="name">The name of the Azure AI Foundry Deployment resource.</param>
    /// <param name="modelName">The name of the model to deploy.</param>
    /// <param name="modelVersion">The version of the model to deploy.</param>
    /// <param name="format">The format of the model to deploy.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddDeployment(this IResourceBuilder<AzureAIFoundryResource> builder, [ResourceName] string name, string modelName, string modelVersion, string format)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(modelName);
        ArgumentException.ThrowIfNullOrEmpty(modelVersion);
        ArgumentException.ThrowIfNullOrEmpty(format);

        var deployment = new AzureAIFoundryDeploymentResource(name, modelName, modelVersion, format, builder.Resource);

        builder.ApplicationBuilder.AddResource(deployment);

        builder.Resource.AddDeployment(deployment);

        var deploymentBuilder = builder.ApplicationBuilder
            .CreateResourceBuilder(deployment);

        if (builder.Resource.IsEmulator)
        {
            deploymentBuilder.AsLocalDeployment(deployment);
        }

        return deploymentBuilder;
    }

    /// <summary>
    /// Adds and returns an Azure AI Foundry Deployment resource to the application model using a <see cref="AIFoundryModel"/>.
    /// </summary>
    /// <param name="builder">The Azure AI Foundry resource builder.</param>
    /// <param name="name">The name of the Azure AI Foundry Deployment resource.</param>
    /// <param name="model">The model descriptor, using the <see cref="AIFoundryModel"/> class like so: <code lang="csharp">aiFoundry.AddDeployment(name: "chat", model: AIFoundryModel.OpenAI.Gpt5Mini)</code></param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <example>
    /// Create a deployment for the OpenAI GTP-5-mini model:
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var aiFoundry = builder.AddAzureAIFoundry("aiFoundry");
    /// var gpt5mini = aiFoundry.AddDeployment("chat", AIFoundryModel.OpenAI.Gpt5Mini);
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddDeployment(this IResourceBuilder<AzureAIFoundryResource> builder, [ResourceName] string name, AIFoundryModel model)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(model.Name);
        ArgumentException.ThrowIfNullOrEmpty(model.Version);
        ArgumentException.ThrowIfNullOrEmpty(model.Format);

        return builder.AddDeployment(name, model.Name, model.Version, model.Format);
    }

    /// <summary>
    /// Allows setting the properties of an Azure AI Foundry Deployment resource.
    /// </summary>
    /// <param name="builder">The Azure AI Foundry Deployment resource builder.</param>
    /// <param name="configure">A method that can be used for customizing the <see cref="AzureAIFoundryDeploymentResource"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> WithProperties(this IResourceBuilder<AzureAIFoundryDeploymentResource> builder, Action<AzureAIFoundryDeploymentResource> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        configure(builder.Resource);

        return builder;
    }

    /// <summary>
    /// Adds a Foundry Local resource to the distributed application builder.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <returns>A resource builder for the Foundry Local resource.</returns>
    public static IResourceBuilder<AzureAIFoundryResource> RunAsFoundryLocal(this IResourceBuilder<AzureAIFoundryResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        var resource = builder.Resource;
        resource.Annotations.Add(new EmulatorResourceAnnotation());

        builder.ApplicationBuilder.Services.AddSingleton(_ => FoundryLocalManager.Instance);

        builder.WithInitializer();

        foreach (var deployment in resource.Deployments)
        {
            var deploymentBuilder = builder.ApplicationBuilder
                .CreateResourceBuilder(deployment);

            deploymentBuilder.AsLocalDeployment(deployment);
        }

        var healthCheckKey = $"{resource.Name}_check";
        builder.ApplicationBuilder.Services.AddHealthChecks()
                .Add(new HealthCheckRegistration(
                    healthCheckKey,
                    sp => new FoundryLocalHealthCheck(),
                    failureStatus: default,
                    tags: default,
                    timeout: default
                    ));

        builder.WithHealthCheck(healthCheckKey);

        return builder;
    }

    /// <summary>
    /// Assigns the specified roles to the given resource, granting it the necessary permissions
    /// on the target Azure AI Foundry resource. This replaces the default role assignments for the resource.
    /// </summary>
    /// <param name="builder">The resource to which the specified roles will be assigned.</param>
    /// <param name="target">The target Azure AI Foundry resource.</param>
    /// <param name="roles">The built-in Cognitive Services roles to be assigned.</param>
    /// <returns>The updated <see cref="IResourceBuilder{T}"/> with the applied role assignments.</returns>
    /// <remarks>
    /// <example>
    /// Assigns the CognitiveServicesOpenAIContributor role to the 'Projects.Api' project.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var aiFoundry = builder.AddAzureAIFoundry("aiFoundry");
    ///
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///   .WithRoleAssignments(aiFoundry, CognitiveServicesBuiltInRole.CognitiveServicesOpenAIContributor)
    ///   .WithReference(aiFoundry);
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<T> WithRoleAssignments<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<AzureAIFoundryResource> target,
        params CognitiveServicesBuiltInRole[] roles)
        where T : IResource
    {
        return builder.WithRoleAssignments(target, CognitiveServicesBuiltInRole.GetBuiltInRoleName, roles);
    }

    private static IResourceBuilder<AzureAIFoundryResource> WithInitializer(this IResourceBuilder<AzureAIFoundryResource> builder)
    {
        return builder.OnInitializeResource((resource, @event, ct)
            => Task.Run(async () =>
            {
                var rns = @event.Services.GetRequiredService<ResourceNotificationService>();
                var logger = @event.Services.GetRequiredService<ResourceLoggerService>().GetLogger(resource);

                var foundryLocalConfig = new Configuration
                {
                    AppName = resource.Name
                };
                await FoundryLocalManager.CreateAsync(foundryLocalConfig, ct).ConfigureAwait(false);
                var manager = FoundryLocalManager.Instance;

                if (manager is null)
                {
                    logger.LogInformation("Foundry Local Manager could not be created.");
                    await rns.PublishUpdateAsync(resource, state => state with
                    {
                        State = KnownResourceStates.FailedToStart,
                        Properties = [.. state.Properties, new(CustomResourceKnownProperties.Source, "Foundry Local")]
                    }).ConfigureAwait(false);
                    return;
                }

                await rns.PublishUpdateAsync(resource, state => state with
                {
                    State = new ResourceStateSnapshot(KnownResourceStates.Starting, KnownResourceStateStyles.Info),
                    Properties = [.. state.Properties, new(CustomResourceKnownProperties.Source, "Foundry Local")]
                }).ConfigureAwait(false);

                try
                {
                    await manager.StartWebServiceAsync(ct).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    logger.LogInformation("Foundry Local could not be started. Ensure it's installed correctly: https://learn.microsoft.com/azure/ai-foundry/foundry-local/get-started (Error: {Error}).", e.Message);
                }

                if (FoundryLocalManager.IsInitialized)
                {
                    resource.EmulatorServiceUri = foundryLocalConfig.Web?.ExternalUrl;

                    await rns.PublishUpdateAsync(resource, state => state with
                    {
                        State = KnownResourceStates.Running,
                        Properties = [.. state.Properties, new(CustomResourceKnownProperties.Source, "Foundry Local")]
                    }).ConfigureAwait(false);
                }
                else
                {
                    await rns.PublishUpdateAsync(resource, state => state with
                    {
                        State = KnownResourceStates.FailedToStart,
                        Properties = [.. state.Properties, new(CustomResourceKnownProperties.Source, "Foundry Local")]
                    }).ConfigureAwait(false);
                }

            }, ct));
    }

    /// <summary>
    /// Configure a deployment for use with Foundry Local
    /// </summary>
    internal static IResourceBuilder<AzureAIFoundryDeploymentResource> AsLocalDeployment(this IResourceBuilder<AzureAIFoundryDeploymentResource> builder, AzureAIFoundryDeploymentResource deployment)
    {
        ArgumentNullException.ThrowIfNull(deployment, nameof(deployment));

        var foundryResource = builder.Resource.Parent;
        builder.ApplicationBuilder.Eventing.Subscribe<ResourceReadyEvent>(foundryResource, (@event, ct) =>
        {
            var rns = @event.Services.GetRequiredService<ResourceNotificationService>();
            var loggerService = @event.Services.GetRequiredService<ResourceLoggerService>();
            var logger = loggerService.GetLogger(deployment);
            var manager = @event.Services.GetRequiredService<FoundryLocalManager>();
            var eventing = @event.Services.GetRequiredService<IDistributedApplicationEventing>();

            var modelName = deployment.ModelName;

            _ = Task.Run(async () =>
            {
                await rns.PublishUpdateAsync(deployment, state => state with
                {
                    State = new ResourceStateSnapshot($"Downloading model {modelName}", KnownResourceStateStyles.Info),
                    Properties = [.. state.Properties, new(CustomResourceKnownProperties.Source, modelName)]
                }).ConfigureAwait(false);

                var catalog = await manager.GetCatalogAsync(ct).ConfigureAwait(false);

                var model = await catalog.GetModelAsync(modelName, ct).ConfigureAwait(false);

                if (model is null)
                {
                    logger.LogInformation("Model {Model} not found in local catalog.", modelName);
                    await rns.PublishUpdateAsync(deployment, state => state with
                    {
                        State = KnownResourceStates.FailedToStart
                    }).ConfigureAwait(false);
                    return;
                }

                if (await model.IsCachedAsync(ct).ConfigureAwait(false))
                {
                    logger.LogInformation("Model {Model} is already cached locally.", modelName);
                }
                else
                {
                    await model.DownloadAsync(async progress =>
                    {
                        logger.LogInformation("Downloading model {Model}: {Progress:F2}%", modelName, progress);
                        await rns.PublishUpdateAsync(deployment, state => state with
                        {
                            State = new ResourceStateSnapshot($"Downloading model {modelName}: {progress:F2}%", KnownResourceStateStyles.Info)
                        }).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

                    logger.LogInformation("Model {Model} downloaded successfully ({ModelId}).", modelName, model.Id);
                }

                deployment.ModelId = model.Id;

                // Re-publish the connection string since the model id is now known
                var connectionStringAvailableEvent = new ConnectionStringAvailableEvent(deployment, @event.Services);
                await eventing.PublishAsync(connectionStringAvailableEvent, ct).ConfigureAwait(false);

                await rns.PublishUpdateAsync(deployment, state => state with
                {
                    Properties = [.. state.Properties, new(CustomResourceKnownProperties.Source, $"{modelName} ({model.Id})")]
                }).ConfigureAwait(false);

                await rns.PublishUpdateAsync(deployment, state => state with
                {
                    State = new ResourceStateSnapshot("Loading model", KnownResourceStateStyles.Info)
                }).ConfigureAwait(false);

                try
                {
                    await model.LoadAsync(ct).ConfigureAwait(false);

                    await rns.PublishUpdateAsync(deployment, state => state with
                    {
                        State = KnownResourceStates.Running
                    }).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    // LoadModelAsync throws IOE when the model is invalid.
                    logger.LogInformation("Failed to start {Model}. Error: {Error}", modelName, e.Message);

                    await rns.PublishUpdateAsync(deployment, state => state with
                    {
                        State = KnownResourceStates.FailedToStart
                    }).ConfigureAwait(false);
                }
            }, ct);

            return Task.CompletedTask;
        });

        var healthCheckKey = $"{deployment.Name}_check";

        builder.ApplicationBuilder.Services.AddHealthChecks()
                .Add(new HealthCheckRegistration(
                    healthCheckKey,
                    sp => new LocalModelHealthCheck(modelId: deployment.ModelId),
                    failureStatus: default,
                    tags: default,
                    timeout: default
                    ));

        builder.WithHealthCheck(healthCheckKey);

        return builder;
    }

    private static void ConfigureInfrastructure(AzureResourceInfrastructure infrastructure)
    {
        var cogServicesAccount = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(infrastructure,
                (identifier, name) =>
                {
                    var resource = CognitiveServicesAccount.FromExisting(identifier);
                    resource.Name = name;
                    return resource;
                },
                (infrastructure) => new CognitiveServicesAccount(infrastructure.AspireResource.GetBicepIdentifier())
                {
                    Kind = "AIServices",
                    Sku = new CognitiveServicesSku()
                    {
                        Name = "S0"
                    },
                    Properties = new CognitiveServicesAccountProperties()
                    {
                        CustomSubDomainName = ToLower(Take(Concat(infrastructure.AspireResource.Name, GetUniqueString(GetResourceGroup().Id)), 24)),
                        PublicNetworkAccess = ServiceAccountPublicNetworkAccess.Enabled,
                        DisableLocalAuth = true
                    },
                    Identity = new ManagedServiceIdentity()
                    {
                        ManagedServiceIdentityType = ManagedServiceIdentityType.SystemAssigned
                    },
                    Tags = { { "aspire-resource-name", infrastructure.AspireResource.Name } }
                });

        infrastructure.Add(new ProvisioningOutput("aiFoundryApiEndpoint", typeof(string))
        {
            Value = (BicepValue<string>)new IndexExpression(
                (BicepExpression)cogServicesAccount.Properties.Endpoints!,
                "AI Foundry API")
        });

        infrastructure.Add(new ProvisioningOutput("endpoint", typeof(string))
        {
            Value = cogServicesAccount.Properties.Endpoint
        });

        infrastructure.Add(new ProvisioningOutput("name", typeof(string)) { Value = cogServicesAccount.Name });

        var resource = (AzureAIFoundryResource)infrastructure.AspireResource;

        CognitiveServicesAccountDeployment? dependency = null;
        foreach (var deployment in resource.Deployments)
        {
            var cdkDeployment = new CognitiveServicesAccountDeployment(Infrastructure.NormalizeBicepIdentifier(deployment.Name))
            {
                Name = deployment.DeploymentName,
                Parent = cogServicesAccount,
                Properties = new CognitiveServicesAccountDeploymentProperties()
                {
                    Model = new CognitiveServicesAccountDeploymentModel()
                    {
                        Name = deployment.ModelName,
                        Version = deployment.ModelVersion,
                        Format = deployment.Format
                    }
                },
                Sku = new CognitiveServicesSku()
                {
                    Name = deployment.SkuName,
                    Capacity = deployment.SkuCapacity
                }
            };
            infrastructure.Add(cdkDeployment);

            // Subsequent deployments need an explicit dependency on the previous one
            // to ensure they are not created in parallel. This is equivalent to @batchSize(1)
            // which can't be defined with the CDK

            if (dependency != null)
            {
                cdkDeployment.DependsOn.Add(dependency);
            }

            dependency = cdkDeployment;
        }
    }

}
