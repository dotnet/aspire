// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for subscribing to <see cref="IDistributedApplicationEvent"/> and <see cref="IDistributedApplicationResourceEvent"/> events.
/// </summary>
public static class DistributedApplicationEventingExtensions
{
    /// <summary>
    /// Subscribes a callback to the <see cref="BeforeStartEvent"/> event within the AppHost.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="callback">A callback to handle the event.</param>
    /// <returns>The <paramref name="builder"/> for chaining.</returns>
    /// <remarks>If you need to ensure you only subscribe to the event once, see <see cref="Lifecycle.IDistributedApplicationEventingSubscriber"/>.</remarks>
    [AspireExport("onBeforeStart", Description = "Subscribes a callback to the BeforeStartEvent event within the AppHost.")]
    public static T OnBeforeStart<T>(this T builder, Func<BeforeStartEvent, CancellationToken, Task> callback)
        where T : IDistributedApplicationBuilder
        => builder.OnApplicationEvent(callback);

    /// <summary>
    /// Subscribes a callback to the <see cref="AfterResourcesCreatedEvent"/> event within the AppHost.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="callback">A callback to handle the event.</param>
    /// <returns>The <paramref name="builder"/> for chaining.</returns>
    /// <remarks>If you need to ensure you only subscribe to the event once, see <see cref="Lifecycle.IDistributedApplicationEventingSubscriber"/>.</remarks>
    [AspireExport("onAfterResourcesCreated", Description = "Subscribes a callback to the AfterResourcesCreatedEvent event within the AppHost.")]
    public static T OnAfterResourcesCreated<T>(this T builder, Func<AfterResourcesCreatedEvent, CancellationToken, Task> callback)
        where T : IDistributedApplicationBuilder
        => builder.OnApplicationEvent(callback);

    /// <summary>
    /// Subscribes a callback to the <see cref="BeforePublishEvent"/> event within the AppHost.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="callback">A callback to handle the event.</param>
    /// <returns>The <paramref name="builder"/> for chaining.</returns>
    /// <remarks>If you need to ensure you only subscribe to the event once, see <see cref="Lifecycle.IDistributedApplicationEventingSubscriber"/>.</remarks>
    [AspireExport("onBeforePublish", Description = "Subscribes a callback to the BeforePublishEvent event within the AppHost.")]
    public static T OnBeforePublish<T>(this T builder, Func<BeforePublishEvent, CancellationToken, Task> callback)
        where T : IDistributedApplicationBuilder
        => builder.OnApplicationEvent(callback);

    /// <summary>
    /// Subscribes a callback to the <see cref="AfterPublishEvent"/> event within the AppHost.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="callback">A callback to handle the event.</param>
    /// <returns>The <paramref name="builder"/> for chaining.</returns>
    /// <remarks>If you need to ensure you only subscribe to the event once, see <see cref="Lifecycle.IDistributedApplicationEventingSubscriber"/>.</remarks>
    [AspireExport("onAfterPublish", Description = "Subscribes a callback to the AfterPublishEvent event within the AppHost.")]
    public static T OnAfterPublish<T>(this T builder, Func<AfterPublishEvent, CancellationToken, Task> callback)
        where T : IDistributedApplicationBuilder
        => builder.OnApplicationEvent(callback);

    /// <summary>
    /// Subscribes a callback to the <see cref="BeforeResourceStartedEvent"/> event within the AppHost.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="callback">A callback to handle the event.</param>
    /// <returns>The <paramref name="builder"/> for chaining.</returns>
    [AspireExport("onBeforeResourceStarted", Description = "Subscribes a callback to the BeforeResourceStartedEvent event of the resource.")]
    public static IResourceBuilder<T> OnBeforeResourceStarted<T>(this IResourceBuilder<T> builder, Func<T, BeforeResourceStartedEvent, CancellationToken, Task> callback)
        where T : IResource
        => builder.OnResourceEvent(callback);

    /// <summary>
    /// Subscribes a callback to the <see cref="ResourceStoppedEvent"/> event for <paramref name="builder"/>.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="callback">A callback to handle the event.</param>
    /// <returns>The <paramref name="builder"/> for chaining.</returns>
    [AspireExport("onResourceStopped", Description = "Subscribes a callback to the ResourceStoppedEvent event of the resource.")]
    public static IResourceBuilder<T> OnResourceStopped<T>(this IResourceBuilder<T> builder, Func<T, ResourceStoppedEvent, CancellationToken, Task> callback)
        where T : IResource
        => builder.OnResourceEvent(callback);

    /// <summary>
    /// Subscribes a callback to the <see cref="ConnectionStringAvailableEvent"/> event for <paramref name="builder"/>.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="callback">A callback to handle the event.</param>
    /// <returns>The <paramref name="builder"/> for chaining.</returns>
    [AspireExport("onConnectionStringAvailable", Description = "Subscribes a callback to the ConnectionStringAvailableEvent event of the resource.")]
    public static IResourceBuilder<T> OnConnectionStringAvailable<T>(this IResourceBuilder<T> builder, Func<T, ConnectionStringAvailableEvent, CancellationToken, Task> callback)
        where T : IResourceWithConnectionString
        => builder.OnResourceEvent(callback);

    /// <summary>
    /// Subscribes a callback to the <see cref="InitializeResourceEvent"/> event for <paramref name="builder"/>.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="callback">A callback to handle the event.</param>
    /// <returns>The <paramref name="builder"/> for chaining.</returns>
    [AspireExport("onInitializeResource", Description = "Subscribes a callback to the InitializeResourceEvent event of the resource.")]
    public static IResourceBuilder<T> OnInitializeResource<T>(this IResourceBuilder<T> builder, Func<T, InitializeResourceEvent, CancellationToken, Task> callback)
        where T : IResource
        => builder.OnResourceEvent(callback);

    /// <summary>
    /// Subscribes a callback to the <see cref="ResourceEndpointsAllocatedEvent"/> event for <paramref name="builder"/>.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="callback">A callback to handle the event.</param>
    /// <returns>The <paramref name="builder"/> for chaining.</returns>
    [AspireExport("onResourceEndpointsAllocated", Description = "Subscribes a callback to the ResourceEndpointsAllocatedEvent event of the resource.")]
    public static IResourceBuilder<T> OnResourceEndpointsAllocated<T>(this IResourceBuilder<T> builder, Func<T, ResourceEndpointsAllocatedEvent, CancellationToken, Task> callback)
        where T : IResourceWithEndpoints
        => builder.OnResourceEvent(callback);

    /// <summary>
    /// Subscribes a callback to the <see cref="ResourceReadyEvent"/> event for <paramref name="builder"/>.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="callback">A callback to handle the event.</param>
    /// <returns>The <paramref name="builder"/> for chaining.</returns>
    [AspireExport("onResourceReady", Description = "Subscribes a callback to the ResourceReadyEvent event of the resource.")]
    public static IResourceBuilder<T> OnResourceReady<T>(this IResourceBuilder<T> builder, Func<T, ResourceReadyEvent, CancellationToken, Task> callback)
        where T : IResource
        => builder.OnResourceEvent(callback);

    private static T OnApplicationEvent<T, TEvent>(this T builder, Func<TEvent, CancellationToken, Task> callback)
        where T : IDistributedApplicationBuilder
        where TEvent : IDistributedApplicationEvent
    {
        builder.Eventing.Subscribe(callback);
        return builder;
    }

    private static IResourceBuilder<TResource> OnResourceEvent<TResource, TEvent>(this IResourceBuilder<TResource> builder, Func<TResource, TEvent, CancellationToken, Task> callback)
        where TResource : IResource
        where TEvent : IDistributedApplicationResourceEvent
    {
        builder.ApplicationBuilder.Eventing.Subscribe<TEvent>(builder.Resource, (evt, ct) => callback(builder.Resource, evt, ct));
        return builder;
    }
}
