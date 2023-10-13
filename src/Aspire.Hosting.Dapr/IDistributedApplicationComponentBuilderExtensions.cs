// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dapr;

namespace Aspire.Hosting;

/// <summary>
/// Extensions to <see cref="IDistributedApplicationResourceBuilder{T}"/> related to Dapr.
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
    public static IDistributedApplicationResourceBuilder<T> WithDaprSidecar<T>(this IDistributedApplicationResourceBuilder<T> builder, string appId) where T : IDistributedApplicationResource
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
    public static IDistributedApplicationResourceBuilder<T> WithDaprSidecar<T>(this IDistributedApplicationResourceBuilder<T> builder, DaprSidecarOptions? options = null) where T : IDistributedApplicationResource
    {
        builder.WithAnnotation(new DaprSidecarAnnotation { Options = options });

        return builder;
    }
}
