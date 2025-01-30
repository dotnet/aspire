// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dapr;

namespace Aspire.Hosting;

/// <summary>
/// Extensions to <see cref="IResourceBuilder{T}"/> related to Dapr.
/// </summary>
public static class IDistributedApplicationResourceBuilderExtensions
{
    /// <summary>
    /// Ensures that a Dapr sidecar is started for the resource.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder instance.</param>
    /// <param name="appId">The ID for the application, used for service discovery.</param>
    /// <returns>The resource builder instance.</returns>
    [Obsolete("The Dapr integration has been migrated to the Community Toolkit. Please use the CommunityToolkit.Aspire.Hosting.Dapr integration.", error: false)]
    public static IResourceBuilder<T> WithDaprSidecar<T>(this IResourceBuilder<T> builder, string appId) where T : IResource
    {
        return builder.WithDaprSidecar(new DaprSidecarOptions { AppId = appId });
    }

    /// <summary>
    /// Ensures that a Dapr sidecar is started for the resource.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder instance.</param>
    /// <param name="options">Options for configuring the Dapr sidecar, if any.</param>
    /// <returns>The resource builder instance.</returns>
    [Obsolete("The Dapr integration has been migrated to the Community Toolkit. Please use the CommunityToolkit.Aspire.Hosting.Dapr integration.", error: false)]
    public static IResourceBuilder<T> WithDaprSidecar<T>(this IResourceBuilder<T> builder, DaprSidecarOptions? options = null) where T : IResource
    {
        return builder.WithDaprSidecar(
            sidecarBuilder =>
            {
                if (options is not null)
                {
                    sidecarBuilder.WithOptions(options);
                }
            });
    }

    /// <summary>
    /// Ensures that a Dapr sidecar is started for the resource.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder instance.</param>
    /// <param name="configureSidecar">A callback that can be use to configure the Dapr sidecar.</param>
    /// <returns>The resource builder instance.</returns>
    [Obsolete("The Dapr integration has been migrated to the Community Toolkit. Please use the CommunityToolkit.Aspire.Hosting.Dapr integration.", error: false)]
    public static IResourceBuilder<T> WithDaprSidecar<T>(this IResourceBuilder<T> builder, Action<IResourceBuilder<IDaprSidecarResource>> configureSidecar) where T : IResource
    {
        // Add Dapr is idempotent, so we can call it multiple times.
        builder.ApplicationBuilder.AddDapr();

        var sidecarBuilder = builder.ApplicationBuilder.AddResource(new DaprSidecarResource($"{builder.Resource.Name}-dapr"))
                                                       .WithInitialState(new()
                                                       {
                                                           Properties = [],
                                                           ResourceType = "DaprSidecar",
                                                           State = KnownResourceStates.Hidden
                                                       });

        configureSidecar(sidecarBuilder);

        return builder.WithAnnotation(new DaprSidecarAnnotation(sidecarBuilder.Resource));
    }

    /// <summary>
    /// Configures a Dapr sidecar with the specified options.
    /// </summary>
    /// <param name="builder">The Dapr sidecar resource builder instance.</param>
    /// <param name="options">Options for configuring the Dapr sidecar.</param>
    /// <returns>The Dapr sidecar resource builder instance.</returns>
    [Obsolete("The Dapr integration has been migrated to the Community Toolkit. Please use the CommunityToolkit.Aspire.Hosting.Dapr integration.", error: false)]
    public static IResourceBuilder<IDaprSidecarResource> WithOptions(this IResourceBuilder<IDaprSidecarResource> builder, DaprSidecarOptions options)
    {
        return builder.WithAnnotation(new DaprSidecarOptionsAnnotation(options));
    }

    /// <summary>
    /// Associates a Dapr component with the Dapr sidecar started for the resource.
    /// </summary>
    /// <typeparam name="TDestination">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder instance.</param>
    /// <param name="component">The Dapr component to use with the sidecar.</param>
    [Obsolete("The Dapr integration has been migrated to the Community Toolkit. Please use the CommunityToolkit.Aspire.Hosting.Dapr integration.", error: false)]
    public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<IDaprComponentResource> component) where TDestination : IResource
    {
        return builder.WithAnnotation(new DaprComponentReferenceAnnotation(component.Resource));
    }
}
