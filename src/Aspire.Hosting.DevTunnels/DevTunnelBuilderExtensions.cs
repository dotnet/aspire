// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.DevTunnels;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for exposing endpoints to the Internet via DevTunnels.
/// </summary>
public static class DevTunnelBuilderExtensions
{
    /// <summary>
    /// Configures the named endpoint to be exposed to the Internet using a DevTunnel.
    /// </summary>
    /// <typeparam name="T">The type of the resource which implements <see cref="IResourceWithEndpoints"/>.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="endpointName">The name of the endpoint.</param>
    /// <param name="configureOptions">Callback to configure options for the DevTunnel.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<T> WithDevTunnel<T>(this IResourceBuilder<T> builder, string endpointName, Action<DevTunnelOptions>? configureOptions = null) where T: IResourceWithEndpoints
    {
        builder.ApplicationBuilder.Services.TryAddLifecycleHook<DevTunnelLifecycleHook>();
        builder.ApplicationBuilder.Services.TryAddSingleton<IDevTunnelTool, DevTunnelTool>();

        if (builder.Resource.Annotations.OfType<DevTunnelAnnotation>().SingleOrDefault(a => a.EndpointAnnotation.Name == endpointName) is not { } annotation)
        {
            var defaultDevTunnelId = DevTunnelResourceNameGenerator.GenerateTunnelName(builder, endpointName);
            var endpointAnnotation = builder.Resource.Annotations.OfType<EndpointAnnotation>().Single(a => a.Name == endpointName);
            annotation = new DevTunnelAnnotation(endpointAnnotation, defaultDevTunnelId);
            builder.WithAnnotation(annotation);
        }

        var devTunnelSidecarResourceName = $"{builder.Resource.Name}-{endpointName}-devtunnel";

        if (!builder.ApplicationBuilder.Resources.Any(r => r.Name == devTunnelSidecarResourceName))
        {
            builder.ApplicationBuilder.AddExecutable(devTunnelSidecarResourceName, "devtunnel", builder.ApplicationBuilder.AppHostDirectory);
        }

        configureOptions?.Invoke(annotation.Options);

        return builder;
    }
}
