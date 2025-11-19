// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.DurableTask;
using Microsoft.Extensions.DependencyInjection;

namespace Azure.Hosting;

/// <summary>
/// Extension methods for adding and configuring Durable Task resources within a distributed application.
/// </summary>
public static class DurableTaskResourceExtensions
{
    /// <summary>
    /// Adds a Durable Task scheduler resource to the distributed application.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The logical name of the scheduler resource.</param>
    /// <returns>An <see cref="IResourceBuilder{TResource}"/> for the scheduler resource.</returns>
    public static IResourceBuilder<DurableTaskSchedulerResource> AddDurableTaskScheduler(this IDistributedApplicationBuilder builder, string name)
    {
        var scheduler = new DurableTaskSchedulerResource(name);
        return builder.AddResource(scheduler);
    }

    /// <summary>
    /// Configures the Durable Task scheduler to use an existing scheduler instance referenced by the provided connection string.
    /// No new scheduler resource is provisioned.
    /// </summary>
    /// <param name="builder">The scheduler resource builder.</param>
    /// <param name="connectionString">The connection string referencing the existing Durable Task scheduler instance.</param>
    /// <returns>The same <see cref="IResourceBuilder{DurableTaskSchedulerResource}"/> instance for fluent chaining.</returns>
    /// <remarks>The existing resource annotation is only applied when the execution context is not in publish mode.</remarks>
    public static IResourceBuilder<DurableTaskSchedulerResource> RunAsExisting(this IResourceBuilder<DurableTaskSchedulerResource> builder, string connectionString)
    {
        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            builder.WithAnnotation(new DurableTaskSchedulerConnectionStringAnnotation(connectionString));
        }

        return builder;
    }

    /// <summary>
    /// Configures the Durable Task scheduler to run using the local emulator (only in non-publish modes).
    /// </summary>
    /// <param name="builder">The resource builder for the scheduler.</param>
    /// <returns>The same <see cref="IResourceBuilder{DurableTaskSchedulerResource}"/> instance for chaining.</returns>
    public static IResourceBuilder<DurableTaskSchedulerResource> RunAsEmulator(this IResourceBuilder<DurableTaskSchedulerResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        // Mark this resource as an emulator for consistent resource identification and tooling support
        builder.WithAnnotation(new EmulatorResourceAnnotation());

        builder.WithEndpoint(name: "grpc", targetPort: 8080)
               .WithHttpEndpoint(name: "http", targetPort: 8081)
               .WithHttpEndpoint(name: "dashboard", targetPort: 8082)
               .WithUrlForEndpoint("dashboard", c => c.DisplayText = "Scheduler Dashboard")
               .WithAnnotation(new ContainerImageAnnotation
               {
                   Registry = DurableTaskSchedulerEmulatorContainerImageTags.Registry,
                   Image = DurableTaskSchedulerEmulatorContainerImageTags.Image,
                   Tag = DurableTaskSchedulerEmulatorContainerImageTags.Tag
               });

        var emulatorResource = new DurableTaskSchedulerEmulatorResource(builder.Resource);

        builder.ApplicationBuilder.CreateResourceBuilder(emulatorResource);

        return builder;
    }

    /// <summary>
    /// Adds a Durable Task hub resource associated with the specified scheduler.
    /// </summary>
    /// <param name="builder">The scheduler resource builder.</param>
    /// <param name="name">The logical name of the task hub resource.</param>
    /// <returns>An <see cref="IResourceBuilder{TResource}"/> for the task hub resource.</returns>
    public static IResourceBuilder<DurableTaskHubResource> AddTaskHub(this IResourceBuilder<DurableTaskSchedulerResource> builder, string name)
    {
        var hub = new DurableTaskHubResource(name, builder.Resource);

        var hubBuilder = builder.ApplicationBuilder.AddResource(hub);

        if (builder.Resource.IsEmulator)
        {
            hubBuilder.OnResourceReady(
                async (r, e, ct) =>
                {
                    var notifications = e.Services.GetRequiredService<ResourceNotificationService>();

                    var url = await ReferenceExpression.Create($"{r.Parent.EmulatorDashboardEndpoint}/subscriptions/default/schedulers/default/taskhubs/{r.Name}").GetValueAsync(ct).ConfigureAwait(false);

                    await notifications.PublishUpdateAsync(r, snapshot => snapshot with
                    {
                        State = KnownResourceStates.Running,
                        Urls = [new("dashboard", url ?? throw new InvalidOperationException("URL cannot be null"), false) { DisplayProperties = new() { DisplayName = "Task Hub Dashboard" } }]
                    }).ConfigureAwait(false);
                });
        }

        return hubBuilder;
    }
}
