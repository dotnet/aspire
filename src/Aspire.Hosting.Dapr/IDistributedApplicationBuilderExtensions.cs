// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dapr;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;
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
    /// <param name="configure">Callback to configure dapr options.</param>
    /// <returns>The distributed application builder instance.</returns>
    [Obsolete("The Dapr integration has been migrated to the Community Toolkit. Please use the CommunityToolkit.Aspire.Hosting.Dapr integration.", error: false)]
    public static IDistributedApplicationBuilder AddDapr(this IDistributedApplicationBuilder builder, Action<DaprOptions>? configure = null)
    {
        if (configure is not null)
        {
            builder.Services.Configure(configure);
        }
        builder.Services.TryAddLifecycleHook<DaprDistributedApplicationLifecycleHook>();

        return builder;
    }

    /// <summary>
    /// Adds a Dapr component to the application model.
    /// </summary>
    /// <param name="builder">The distributed application builder instance.</param>
    /// <param name="name">The name of the component.</param>
    /// <param name="type">The type of the component. This can be a generic "state" or "pubsub" string, to have Aspire choose an appropriate type when running or deploying.</param>
    /// <param name="options">Options for configuring the component, if any.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    [Obsolete("The Dapr integration has been migrated to the Community Toolkit. Please use the CommunityToolkit.Aspire.Hosting.Dapr integration.", error: false)]
    public static IResourceBuilder<IDaprComponentResource> AddDaprComponent(this IDistributedApplicationBuilder builder, [ResourceName] string name, string type, DaprComponentOptions? options = null)
    {
        var resource = new DaprComponentResource(name, type) { Options = options };
        return builder
            .AddResource(resource)
            .WithInitialState(new()
            {
                Properties = [],
                ResourceType = "DaprComponent",
                State = KnownResourceStates.Hidden
            })
            .WithAnnotation(new ManifestPublishingCallbackAnnotation(context => WriteDaprComponentResourceToManifest(context, resource)));
    }

    /// <summary>
    /// Adds a "generic" Dapr pub-sub component to the application model. Aspire will configure an appropriate type when running or deploying.
    /// </summary>
    /// <param name="builder">The distributed application builder instance.</param>
    /// <param name="name">The name of the component.</param>
    /// <param name="options">Options for configuring the component, if any.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    [Obsolete("The Dapr integration has been migrated to the Community Toolkit. Please use the CommunityToolkit.Aspire.Hosting.Dapr integration.", error: false)]
    public static IResourceBuilder<IDaprComponentResource> AddDaprPubSub(this IDistributedApplicationBuilder builder, [ResourceName] string name, DaprComponentOptions? options = null)
    {
        return builder.AddDaprComponent(name, DaprConstants.BuildingBlocks.PubSub, options);
    }

    /// <summary>
    /// Adds a Dapr state store component to the application model. Aspire will configure an appropriate type when running or deploying.
    /// </summary>
    /// <param name="builder">The distributed application builder instance.</param>
    /// <param name="name">The name of the component.</param>
    /// <param name="options">Options for configuring the component, if any.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    [Obsolete("The Dapr integration has been migrated to the Community Toolkit. Please use the CommunityToolkit.Aspire.Hosting.Dapr integration.", error: false)]
    public static IResourceBuilder<IDaprComponentResource> AddDaprStateStore(this IDistributedApplicationBuilder builder, [ResourceName] string name, DaprComponentOptions? options = null)
    {
        return builder.AddDaprComponent(name, DaprConstants.BuildingBlocks.StateStore, options);
    }

    [Obsolete("The Dapr integration has been migrated to the Community Toolkit. Please use the CommunityToolkit.Aspire.Hosting.Dapr integration.", error: false)]
    private static void WriteDaprComponentResourceToManifest(ManifestPublishingContext context, DaprComponentResource resource)
    {
        context.Writer.WriteString("type", "dapr.component.v0");
        context.Writer.WriteStartObject("daprComponent");

        if (resource.Options?.LocalPath is { } localPath)
        {
            context.Writer.TryWriteString("localPath", context.GetManifestRelativePath(localPath));
        }
        context.Writer.WriteString("type", resource.Type);

        context.Writer.WriteEndObject();
    }
}
