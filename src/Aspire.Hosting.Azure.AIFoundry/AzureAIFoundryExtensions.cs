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
        return builder.AddResource(resource);
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

        builder.ApplicationBuilder.Services.AddSingleton<FoundryLocalManager>();

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
                    sp => new FoundryLocalHealthCheck(sp.GetRequiredService<FoundryLocalManager>()),
                    failureStatus: default,
                    tags: default,
                    timeout: default
                    ));

        builder.WithHealthCheck(healthCheckKey);

        return builder;
    }

    private static IResourceBuilder<AzureAIFoundryResource> WithInitializer(this IResourceBuilder<AzureAIFoundryResource> builder)
    {
        return builder.OnInitializeResource((resource, @event, ct)
            => Task.Run(async () =>
            {
                var rns = @event.Services.GetRequiredService<ResourceNotificationService>();
                var manager = @event.Services.GetRequiredService<FoundryLocalManager>();
                var logger = @event.Services.GetRequiredService<ResourceLoggerService>().GetLogger(resource);

                resource.ApiKey = manager.ApiKey;

                await rns.PublishUpdateAsync(resource, state => state with
                {
                    State = new ResourceStateSnapshot(KnownResourceStates.Starting, KnownResourceStateStyles.Info)
                }).ConfigureAwait(false);

                try
                {
                    await manager.StartServiceAsync(ct).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    logger.LogInformation("Foundry Local could not be started. Ensure it's installed correctly: https://learn.microsoft.com/azure/ai-foundry/foundry-local/get-started (Error: {Error}).", e.Message);
                }

                if (manager.IsServiceRunning)
                {
                    resource.EmulatorServiceUri = manager.Endpoint;

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

        builder.OnResourceReady((foundryResource, @event, ct) =>
        {
            var rns = @event.Services.GetRequiredService<ResourceNotificationService>();
            var loggerService = @event.Services.GetRequiredService<ResourceLoggerService>();
            var logger = loggerService.GetLogger(deployment);
            var manager = @event.Services.GetRequiredService<FoundryLocalManager>();
            var eventing = @event.Services.GetRequiredService<IDistributedApplicationEventing>();

            var model = deployment.ModelName;

            _ = Task.Run(async () =>
            {
                await rns.PublishUpdateAsync(deployment, state => state with
                {
                    State = new ResourceStateSnapshot($"Downloading model {model}", KnownResourceStateStyles.Info),
                    Properties = [.. state.Properties, new(CustomResourceKnownProperties.Source, model)]
                }).ConfigureAwait(false);

                var result = manager.DownloadModelWithProgressAsync(model, ct: ct);

                await foreach (var progress in result.ConfigureAwait(false))
                {
                    if (progress.IsCompleted && progress.ModelInfo is not null)
                    {
                        deployment.DeploymentName = progress.ModelInfo.ModelId;
                        logger.LogInformation("Model {Model} downloaded successfully ({ModelId}).", model, deployment.DeploymentName);

                        // Re-publish the connection string since the model id is now known
                        var connectionStringAvailableEvent = new ConnectionStringAvailableEvent(deployment, @event.Services);
                        await eventing.PublishAsync(connectionStringAvailableEvent, ct).ConfigureAwait(false);

                        await rns.PublishUpdateAsync(deployment, state => state with
                        {
                            Properties = [.. state.Properties, new(CustomResourceKnownProperties.Source, $"{model} ({progress.ModelInfo.ModelId})")]
                        }).ConfigureAwait(false);

                        await rns.PublishUpdateAsync(deployment, state => state with
                        {
                            State = new ResourceStateSnapshot("Loading model", KnownResourceStateStyles.Info)
                        }).ConfigureAwait(false);

                        try
                        {
                            _ = await manager.LoadModelAsync(deployment.DeploymentName, ct: ct).ConfigureAwait(false);

                            await rns.PublishUpdateAsync(deployment, state => state with
                            {
                                State = KnownResourceStates.Running
                            }).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            // LoadModelAsync throws IOE when the model is invalid.
                            logger.LogInformation("Failed to start {Model}. Error: {Error}", model, e.Message);

                            await rns.PublishUpdateAsync(deployment, state => state with
                            {
                                State = KnownResourceStates.FailedToStart
                            }).ConfigureAwait(false);
                        }
                    }
                    else if (progress.IsCompleted && !string.IsNullOrEmpty(progress.ErrorMessage))
                    {
                        logger.LogInformation("Failed to start {Model}. Error: {Error}", model, progress.ErrorMessage);
                        await rns.PublishUpdateAsync(deployment, state => state with
                        {
                            State = KnownResourceStates.FailedToStart
                        }).ConfigureAwait(false);
                    }
                    else
                    {
                        logger.LogInformation("Downloading model {Model}: {Progress:F2}%", model, progress.Percentage);
                        await rns.PublishUpdateAsync(deployment, state => state with
                        {
                            State = new ResourceStateSnapshot($"Downloading model {model}: {progress.Percentage:F2}%", KnownResourceStateStyles.Info)
                        }).ConfigureAwait(false);
                    }
                }
            }, ct);

            return Task.CompletedTask;
        });

        var healthCheckKey = $"{deployment.Name}_check";

        builder.ApplicationBuilder.Services.AddHealthChecks()
                .Add(new HealthCheckRegistration(
                    healthCheckKey,
                    sp => new LocalModelHealthCheck(modelAlias: deployment.ModelName, sp.GetRequiredService<FoundryLocalManager>()),
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
                    Name = Take(Interpolate($"{infrastructure.AspireResource.GetBicepIdentifier()}{GetUniqueString(GetResourceGroup().Id)}"), 64),
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

        var inferenceEndpoint = (BicepValue<string>)new IndexExpression(
            (BicepExpression)cogServicesAccount.Properties.Endpoints!,
            "AI Foundry API");
        infrastructure.Add(new ProvisioningOutput("aiFoundryApiEndpoint", typeof(string))
        {
            Value = inferenceEndpoint
        });

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
