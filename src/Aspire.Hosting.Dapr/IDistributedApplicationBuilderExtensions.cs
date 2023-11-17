// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dapr;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

/// <summary>
/// Extensions to <see cref="IDistributedApplicationBuilder"/> related to Dapr.
/// </summary>
public static class IDistributedApplicationBuilderExtensions
{
    /// <summary>
    /// Adds Dapr support to Aspire, including the ability to add Dapr sidecar to application resource.
    /// </summary>
    /// <param name="builder">The distributed application builder instance.</param>
    /// <param name="options">Options for configuring Dapr, if any.</param>
    /// <returns>The distributed application builder instance.</returns>
    public static IDistributedApplicationBuilder AddDapr(this IDistributedApplicationBuilder builder, DaprOptions? options = null)
    {
        builder.Services.AddSingleton(options ?? new DaprOptions());
        builder.Services.AddLifecycleHook<DaprDistributedApplicationLifecycleHook>();
        builder.Services.AddSingleton<DaprPortManager>();

        return builder;
    }

    public static IResourceBuilder<IDaprComponentResource> AddDaprComponent(this IDistributedApplicationBuilder builder, string name, string type, DaprComponentOptions? options = null)
    {
        var resource = new DaprComponentResource(name, type) { Options = options };

        return builder
            .AddResource(resource)
            .WithAnnotation(new ManifestPublishingCallbackAnnotation(writer => WriteDaprComponentResourceToManifest(writer, resource)));
    }

    public static IResourceBuilder<IDaprComponentResource> AddDaprStateStore(this IDistributedApplicationBuilder builder, string name)
    {
        return builder.AddDaprComponent(name, DaprConstants.BuildingBlocks.StateStore);
    }

    private static void WriteDaprComponentResourceToManifest(Utf8JsonWriter writer, DaprComponentResource resource)
    {
        writer.WriteString("type", "dapr.component.v0");
        writer.WriteString("componentType", resource.Type);
    }
}
