// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dapr;

/// <summary>
/// Extensions to <see cref="IDistributedApplicationComponentBuilder{T}"/> related to Dapr.
/// </summary>
public static class IDistributedApplicationComponentBuilderExtensions
{
    /// <summary>
    /// Ensures that a Dapr sidecar is started for the component.
    /// </summary>
    /// <typeparam name="T">The type of the component.</typeparam>
    /// <param name="builder">The component builder instance.</param>
    /// <param name="appId">The ID for the application, used for service discovery.</param>
    /// <returns>The component builder instance.</returns>
    public static IDistributedApplicationComponentBuilder<T> WithDaprSidecar<T>(this IDistributedApplicationComponentBuilder<T> builder, string appId) where T : IDistributedApplicationComponent
    {
        return builder.WithDaprSidecar(new DaprSidecarOptions { AppId = appId });
    }

    /// <summary>
    /// Ensures that a Dapr sidecar is started for the component.
    /// </summary>
    /// <typeparam name="T">The type of the component.</typeparam>
    /// <param name="builder">The component builder instance.</param>
    /// <param name="options">Options for configuring the Dapr sidecar, if any.</param>
    /// <returns>The component builder instance.</returns>
    public static IDistributedApplicationComponentBuilder<T> WithDaprSidecar<T>(this IDistributedApplicationComponentBuilder<T> builder, DaprSidecarOptions? options = null) where T : IDistributedApplicationComponent
    {
        builder.WithAnnotation(new DaprSidecarAnnotation { Options = options });

        return builder;
    }
}
