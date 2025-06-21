// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.AIFoundry;
using Aspire.Hosting.Azure.Internal;
using Aspire.Hosting.Eventing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for configuring and managing Foundry Local resources.
/// </summary>
public static partial class AzureAIFoundryLocalResourceExtensions
{
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

        var azureResource = builder.Resource;
        builder.ApplicationBuilder.Resources.Remove(azureResource);

        var resource = new AzureAIFoundryResource(azureResource.Name, c => { }) { IsLocal = true };
        builder.ApplicationBuilder.AddResource(resource);

        foreach (var deployment in azureResource.Deployments)
        {
            resource.AddDeployment(deployment);
        }

        var resourceBuilder = builder.ApplicationBuilder
            .CreateResourceBuilder(resource);

        resourceBuilder.ApplicationBuilder.Services.AddSingleton<FoundryLocalManager>();

        resourceBuilder
            .WithHttpEndpoint(env: "PORT", isProxied: false, port: 6914, name: AzureAIFoundryResource.PrimaryEndpointName)
            .WithExternalHttpEndpoints()
            //.WithAIFoundryLocalDefaults()
            .WithInitializer()

            // Ensure that when the DCP exits the Foundry Local service is stopped.
            .EnsureResourceStops();

        foreach (var deployment in resource.Deployments)
        {
            var deploymentBuilder = resourceBuilder.ApplicationBuilder
                .CreateResourceBuilder(deployment);

            deploymentBuilder.AsLocalDeployment(deployment);

            deployment.Parent = resource; // Ensure the deployment has the correct parent reference
        }

        var healthCheckKey = $"{resource.Name}_check";
        resourceBuilder.ApplicationBuilder.Services.AddHealthChecks()
                .Add(new HealthCheckRegistration(
                    healthCheckKey,
                    sp => new FoundryHealthCheck(sp.GetRequiredService<FoundryLocalManager>()),
                    failureStatus: default,
                    tags: default,
                    timeout: default
                    ));

        resourceBuilder.WithHealthCheck(healthCheckKey);

        return resourceBuilder;
    }

    private static IResourceBuilder<AzureAIFoundryResource> WithInitializer(this IResourceBuilder<AzureAIFoundryResource> builder)
    {
        builder.ApplicationBuilder.Eventing.Subscribe<InitializeResourceEvent>(builder.Resource, (@event, ct)
            => Task.Run(async () =>
            {
                var resource = (AzureAIFoundryResource)@event.Resource;
                var rns = @event.Services.GetRequiredService<ResourceNotificationService>();
                var manager = @event.Services.GetRequiredService<FoundryLocalManager>();

                await rns.PublishUpdateAsync(resource, state => state with
                {
                    State = new ResourceStateSnapshot(KnownResourceStates.Starting, KnownResourceStateStyles.Info)
                }).ConfigureAwait(false);

                await manager.StartServiceAsync(ct).ConfigureAwait(false);

                resource.ApiKey = manager.ApiKey;

                if (manager.IsServiceRunning)
                {
                    if (resource.TryGetLastAnnotation<EndpointAnnotation>(out var endpoint))
                    {
                        endpoint.AllocatedEndpoint = new AllocatedEndpoint(
                            new EndpointAnnotation(
                                System.Net.Sockets.ProtocolType.Tcp,
                                manager.Endpoint.Scheme,
                                "http",
                                "http",
                                manager.Endpoint.Port,
                                manager.Endpoint.Port,
                                false,
                                false),
                            manager.Endpoint.ToString(),
                            manager.Endpoint.Port);
                    }

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

        return builder;
    }

    /// <summary>
    /// Ensures that the resource stops when the application is shutting down.
    /// </summary>
    /// <param name="resource">The resource builder for the Foundry Local resource.</param>
    /// <returns>The updated resource builder.</returns>
    private static IResourceBuilder<AzureAIFoundryResource> EnsureResourceStops(this IResourceBuilder<AzureAIFoundryResource> resource)
    {
        resource.ApplicationBuilder.Eventing.Subscribe<ResourceReadyEvent>(resource.Resource, static (@event, ct) =>
        {
            _ = Task.Run(async () =>
            {
                var rns = @event.Services.GetRequiredService<ResourceNotificationService>();
                var manager = @event.Services.GetRequiredService<FoundryLocalManager>();

                await rns.WaitForResourceAsync(@event.Resource.Name, KnownResourceStates.Finished, ct).ConfigureAwait(false);

                await manager.StopServiceAsync(ct).ConfigureAwait(false);
            }, ct);

            return Task.CompletedTask;
        });

        return resource;
    }

    /// <summary>
    /// Configure a deployment for use with Foundry Local
    /// </summary>
    internal static IResourceBuilder<AzureAIFoundryDeploymentResource> AsLocalDeployment(this IResourceBuilder<AzureAIFoundryDeploymentResource> builder, AzureAIFoundryDeploymentResource deployment)
    {
        ArgumentNullException.ThrowIfNull(deployment, nameof(deployment));

        var azureFoundryResource = builder.Resource.Parent;

        builder.ApplicationBuilder.Eventing.Subscribe<ResourceReadyEvent>(azureFoundryResource, (@event, ct) =>
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

                var result = manager.DownloadModelWithProgressAsync(model, ct: ct) ?? throw new InvalidOperationException($"Failed to download model {model}.");

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

                        _ = await manager.LoadModelAsync(deployment.DeploymentName, ct: ct).ConfigureAwait(false);

                        await rns.PublishUpdateAsync(deployment, state => state with
                        {
                            State = new ResourceStateSnapshot(KnownResourceStates.Running, KnownResourceStateStyles.Success)
                        }).ConfigureAwait(false);
                    }
                    else if (progress.IsCompleted && !string.IsNullOrEmpty(progress.ErrorMessage))
                    {
                        logger.LogInformation("Failed to start {Model}. Error: {Error}", model, progress.ErrorMessage);
                        await rns.PublishUpdateAsync(deployment, state => state with
                        {
                            State = new ResourceStateSnapshot(KnownResourceStates.FailedToStart, KnownResourceStateStyles.Error)
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
                    sp => new ModelHealthCheck(modelAlias: deployment.ModelName, sp.GetRequiredService<FoundryLocalManager>()),
                    failureStatus: default,
                    tags: default,
                    timeout: default
                    ));

        builder.WithHealthCheck(healthCheckKey);

        return builder;
    }
}
