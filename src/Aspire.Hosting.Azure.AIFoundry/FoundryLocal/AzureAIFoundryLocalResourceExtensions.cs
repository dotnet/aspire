// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.AIFoundry;
using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Provides extension methods for configuring and managing Azure AI Foundry local resources.
/// </summary>
public static partial class AzureAIFoundryLocalResourceExtensions
{
    /// <summary>
    /// Adds an Azure AI Foundry local resource to the distributed application builder.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <returns>A resource builder for the Azure AI Foundry local resource.</returns>
    public static IResourceBuilder<AzureAIFoundryResource> RunAsFoundryLocal(this IResourceBuilder<AzureAIFoundryResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        if (builder.ApplicationBuilder.Resources.OfType<AzureAIFoundryLocalResource>().SingleOrDefault() is { } existingResource)
        {
            builder.ApplicationBuilder.CreateResourceBuilder(existingResource);
            return builder;
        }

        builder.ApplicationBuilder.Services.AddSingleton<FoundryLocalManager>();

        builder.ApplicationBuilder.Resources.Remove(builder.Resource);

        var name = builder.Resource.Name + "-local";
        AzureAIFoundryLocalResource resource = new(name, builder.Resource);
        var localBuilder = builder.ApplicationBuilder.AddResource(resource)
            .WithHttpEndpoint(env: "PORT", isProxied: false, port: 6914, name: AzureAIFoundryLocalResource.PrimaryEndpointName)
            .WithExternalHttpEndpoints()
            //.WithAIFoundryLocalDefaults()
            .WithInitializer()

            // Ensure that when the DCP exits the Foundry Local service is stopped.
            .EnsureResourceStops();

        builder.Resource.SetInnerResource(resource);

        // Move any existing deployments from this resource to the new Local Foundry one.
        var deployments = builder.ApplicationBuilder.Resources
            .OfType<AzureAIFoundryDeploymentResource>()
            .Where(r => r.Parent == builder.Resource)
            .ToArray();

        foreach (var deployment in deployments)
        {
            builder.ApplicationBuilder.Resources.Remove(deployment);
            localBuilder.AddModel(deployment.Name, deployment.ModelName);
        }

        return builder;
    }

    private static IResourceBuilder<AzureAIFoundryLocalResource> WithInitializer(this IResourceBuilder<AzureAIFoundryLocalResource> builder)
    {
        builder.ApplicationBuilder.Eventing.Subscribe<InitializeResourceEvent>(builder.Resource, (@event, ct)
            => Task.Run(async () =>
            {
                var resource = (AzureAIFoundryLocalResource)@event.Resource;
                var rns = @event.Services.GetRequiredService<ResourceNotificationService>();
                var manager = @event.Services.GetRequiredService<FoundryLocalManager>();

                await rns.PublishUpdateAsync(resource, state => state with
                {
                    State = new ResourceStateSnapshot(KnownResourceStates.Starting, KnownResourceStateStyles.Info)
                }).ConfigureAwait(false);

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                try
                {
                    await manager.StartServiceAsync(cts.Token).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    await manager.StartServiceAsync(ct).ConfigureAwait(false);
                }

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
    /// <param name="resource">The resource builder for the Azure AI Foundry local resource.</param>
    /// <returns>The updated resource builder.</returns>
    private static IResourceBuilder<AzureAIFoundryLocalResource> EnsureResourceStops(this IResourceBuilder<AzureAIFoundryLocalResource> resource)
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
    /// Adds a model to the Azure AI Foundry local resource.
    /// </summary>
    /// <param name="builder">The resource builder for the Azure AI Foundry local resource.</param>
    /// <param name="name">The name of the model resource.</param>
    /// <param name="model">The model identifier.</param>
    /// <returns>A resource builder for the Azure AI Foundry local model resource.</returns>
    public static IResourceBuilder<AzureAIFoundryLocalModelResource> AddModel(this IResourceBuilder<AzureAIFoundryLocalResource> builder, [ResourceName] string name, string model)
    {
        ArgumentException.ThrowIfNullOrEmpty(model, nameof(model));

        var modelResource = new AzureAIFoundryLocalModelResource(name, model, builder.Resource);

        builder.Resource.AddModel(modelResource);

        builder.ApplicationBuilder.Eventing.Subscribe<ResourceReadyEvent>(builder.Resource, (@event, ct) =>
        {
            var rns = @event.Services.GetRequiredService<ResourceNotificationService>();
            var loggerService = @event.Services.GetRequiredService<ResourceLoggerService>();
            var logger = loggerService.GetLogger(modelResource);
            var manager = @event.Services.GetRequiredService<FoundryLocalManager>();

            _ = Task.Run(async () =>
            {
                await rns.PublishUpdateAsync(modelResource, state => state with
                {
                    State = new ResourceStateSnapshot($"Downloading model {model}", KnownResourceStateStyles.Info),
                    Properties = [.. state.Properties, new(CustomResourceKnownProperties.Source, model)]
                }).ConfigureAwait(false);

                var result = manager.DownloadModelWithProgressAsync(model, ct: ct) ?? throw new InvalidOperationException($"Failed to download model {model}.");

                await foreach (var progress in result.ConfigureAwait(false))
                {
                    if (progress.IsCompleted && progress.ModelInfo is not null)
                    {
                        var modelInfo = progress.ModelInfo;
                        logger.LogInformation("Model {Model} downloaded successfully.", model);

                        await rns.PublishUpdateAsync(modelResource, state => state with
                        {
                            State = new ResourceStateSnapshot("Loading model", KnownResourceStateStyles.Info)
                        }).ConfigureAwait(false);

                        _ = await manager.LoadModelAsync(modelInfo.ModelId, ct: ct).ConfigureAwait(false);

                        await rns.PublishUpdateAsync(modelResource, state => state with
                        {
                            State = new ResourceStateSnapshot(KnownResourceStates.Running, KnownResourceStateStyles.Success)
                        }).ConfigureAwait(false);

                        modelResource.ModelId = modelInfo.ModelId;
                    }
                    else if (progress.IsCompleted && !string.IsNullOrEmpty(progress.ErrorMessage))
                    {
                        logger.LogInformation("Failed to start {Model}. Error: {Error}", model, progress.ErrorMessage);
                        await rns.PublishUpdateAsync(modelResource, state => state with
                        {
                            State = new ResourceStateSnapshot(KnownResourceStates.FailedToStart, KnownResourceStateStyles.Error)
                        }).ConfigureAwait(false);
                    }
                    else
                    {
                        logger.LogInformation("Downloading model {model}: {progress.Percentage}%", model, progress.Percentage);
                        await rns.PublishUpdateAsync(modelResource, state => state with
                        {
                            State = new ResourceStateSnapshot($"Downloading model {model}: {progress.Percentage}%", KnownResourceStateStyles.Info)
                        }).ConfigureAwait(false);
                    }
                }
            }, ct);

            return Task.CompletedTask;
        });

        var healthCheckKey = $"{name}_check";
        builder.ApplicationBuilder.Services.AddHealthChecks()
                .Add(new HealthCheckRegistration(
                    healthCheckKey,
                    sp => new ModelHealthCheck(model, sp.GetRequiredService<FoundryLocalManager>()),
                    failureStatus: default,
                    tags: default,
                    timeout: default
                    ));

        return builder.ApplicationBuilder
            .AddResource(modelResource)
            .WithParentRelationship(builder.Resource)
            .WithHealthCheck(healthCheckKey);
    }

    ///// <summary>
    ///// Configures the resource builder with default settings for Azure AI Foundry local resources.
    ///// </summary>
    ///// <param name="resource">The resource builder for the Azure AI Foundry local resource.</param>
    ///// <returns>The updated resource builder.</returns>
    //private static IResourceBuilder<AzureAIFoundryLocalResource> WithAIFoundryLocalDefaults(this IResourceBuilder<AzureAIFoundryLocalResource> resource)
    //    => resource.WithHttpHealthCheck("/openai/status");
}
