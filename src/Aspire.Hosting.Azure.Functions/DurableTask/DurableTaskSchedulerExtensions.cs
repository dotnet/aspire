// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using System.Diagnostics;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Provides extension methods for adding and configuring Durable Task Scheduler resources to the application model.
/// </summary>
public static class DurableTaskSchedulerExtensions
{
    /// <summary>
    /// Adds a Durable Task Scheduler resource to the application model.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="configure">(Optional) Callback that exposes the resource allowing for customization.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{DurableTaskSchedulerResource}" />.</returns>
    public static IResourceBuilder<DurableTaskSchedulerResource> AddDurableTaskScheduler(this IDistributedApplicationBuilder builder, [ResourceName] string name, Action<IResourceBuilder<DurableTaskSchedulerResource>>? configure = null)
    {
        DurableTaskSchedulerResource resource = new(name);

        var resourceBuilder = builder.AddResource(resource);

        configure?.Invoke(resourceBuilder);

        return resourceBuilder;
    }

    /// <summary>
    /// Configures a Durable Task Scheduler resource to be emulated.
    /// </summary>
    /// <param name="builder">The Durable Task Scheduler resource builder.</param>
    /// <param name="configureContainer">Callback that exposes the underlying container used for emulation allowing for customization.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{DurableTaskSchedulerResource}" />.</returns>
    /// <remarks>
    /// This version of the package defaults to the <inheritdoc cref="DurableTaskConstants.Scheduler.Emulator.Container.Tag" /> tag of the <inheritdoc cref="DurableTaskConstants.Scheduler.Emulator.Container.Image" /> container image in the <inheritdoc cref="DurableTaskConstants.Scheduler.Emulator.Container.Registry" /> registry.
    /// </remarks>
    /// <example>
    /// The following example creates a Durable Task Scheduler resource that runs locally in an emulator and referencing that resource in a .NET project.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var scheduler = builder.AddDurableTaskScheduler("scheduler")
    ///                        .RunAsEmulator();
    ///
    /// var taskHub = scheduler.AddDurableTaskHub("taskhub");
    ///
    /// builder.AddProject&lt;Projects.MyApp&gt;("myapp")
    ///        .WithReference(taskHub);
    /// 
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<DurableTaskSchedulerResource> RunAsEmulator(this IResourceBuilder<DurableTaskSchedulerResource> builder, Action<IResourceBuilder<DurableTaskSchedulerEmulatorResource>>? configureContainer = null)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        builder
            .WithDashboard();

        //
        // Add dashboards for existing task hubs (not already marked to have a dashboard annotation).
        //

        var existingTaskHubs =
            builder
                .ApplicationBuilder
                .Resources
                .OfType<DurableTaskHubResource>()
                .Where(taskHub => taskHub.Parent == builder.Resource)
                .Where(taskHub => !taskHub.HasAnnotationOfType<DurableTaskSchedulerDashboardAnnotation>());

        foreach (var taskHub in existingTaskHubs)
        {
            builder.ApplicationBuilder.CreateResourceBuilder(taskHub).WithDashboard();
        }

        var surrogateBuilder = builder.ApplicationBuilder.CreateResourceBuilder(new DurableTaskSchedulerEmulatorResource(builder.Resource))
            .WithHttpEndpoint(name: DurableTaskConstants.Scheduler.Emulator.Endpoints.Worker, targetPort: 8080)
            .WithHttpEndpoint(name: DurableTaskConstants.Scheduler.Emulator.Endpoints.Dashboard, targetPort: 8082)
            .WithEnvironment(
                async (EnvironmentCallbackContext context) =>
                    {
                        var nameTasks =
                            builder
                                .ApplicationBuilder
                                .Resources
                                .OfType<DurableTaskHubResource>()
                                .Where(r => r.Parent == builder.Resource)
                                .Select(r => r.TaskHubNameExpression)
                                .Select(async r => await r.GetValueAsync(context.CancellationToken).ConfigureAwait(false))
                                .ToList();

                        await Task.WhenAll(nameTasks).ConfigureAwait(false);

                        var taskHubNames = nameTasks
                            .Select(r => r.Result)
                            .Where(r => r is not null)
                            .Distinct()
                            .ToList();

                        if (taskHubNames.Any())
                        {
                            context.EnvironmentVariables.Add("DTS_TASK_HUB_NAMES", String.Join(",", taskHubNames));
                        }
                    })
            .WithAnnotation(
                new ContainerImageAnnotation
                {
                    Registry = DurableTaskConstants.Scheduler.Emulator.Container.Registry,
                    Image = DurableTaskConstants.Scheduler.Emulator.Container.Image,
                    Tag = DurableTaskConstants.Scheduler.Emulator.Container.Tag
                });

        configureContainer?.Invoke(surrogateBuilder);

        if (surrogateBuilder.Resource.UseDynamicTaskHubs)
        {
            surrogateBuilder.WithEnvironment(
                (EnvironmentCallbackContext context) =>
                {
                    context.EnvironmentVariables.Add("DTS_USE_DYNAMIC_TASK_HUBS", "true");
                });
        }

        builder.Resource.Authentication ??= DurableTaskSchedulerAuthentication.None;

        return builder;
    }

    /// <summary>
    /// Marks the resource as an existing Durable Task Scheduler instance when the application is running.
    /// </summary>
    /// <param name="builder">The Durable Task Scheduler resource builder.</param>
    /// <param name="connectionString">The connection string to the existing Durable Task Scheduler instance.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{DurableTaskSchedulerResource}" />.</returns>
    /// <example>
    /// The following example creates a preexisting Durable Task Scheduler resource configured via external parameters and referencing that resource in a .NET project.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var scheduler = builder.AddDurableTaskScheduler("scheduler")
    ///                        .RunAsExisting(builder.AddParameter("scheduler-connection-string"));
    ///
    /// var taskHub =
    ///     scheduler.AddDurableTaskHub("taskhub")
    ///              .WithTaskHubName(builder.AddParameter("taskhub-name"));
    ///
    /// builder.AddProject&lt;Projects.MyApp&gt;("myapp")
    ///        .WithReference(taskHub);
    /// 
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<DurableTaskSchedulerResource> RunAsExisting(this IResourceBuilder<DurableTaskSchedulerResource> builder, IResourceBuilder<ParameterResource> connectionString)
    {
        return builder.RunAsExisting(ReferenceExpression.Create($"{connectionString}"));
    }

    /// <summary>
    /// Marks the resource as an existing Durable Task Scheduler instance when the application is running.
    /// </summary>
    /// <param name="builder">The Durable Task Scheduler resource builder.</param>
    /// <param name="connectionString">The connection string to the existing Durable Task Scheduler instance.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{DurableTaskSchedulerResource}" />.</returns>
    /// <example>
    /// The following example creates a preexisting Durable Task Scheduler resource configured via strings and referencing that resource in a .NET project.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// string connectionString = "...";
    /// 
    /// var scheduler = builder.AddDurableTaskScheduler("scheduler")
    ///                        .RunAsExisting(connectionString);
    ///
    /// string taskHubName = "...";
    /// 
    /// var taskHub =
    ///     scheduler.AddDurableTaskHub("taskhub")
    ///              .WithTaskHubName(taskHubName);
    ///
    /// builder.AddProject&lt;Projects.MyApp&gt;("myapp")
    ///        .WithReference(taskHub);
    /// 
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<DurableTaskSchedulerResource> RunAsExisting(this IResourceBuilder<DurableTaskSchedulerResource> builder, string connectionString)
    {
        return builder.RunAsExisting(ReferenceExpression.Create($"{connectionString}"));
    }

    /// <summary>
    /// Marks the resource as an existing Durable Task Scheduler instance when the application is running.
    /// </summary>
    /// <param name="builder">The Durable Task Scheduler resource builder.</param>
    /// <param name="connectionString">The connection string to the existing Durable Task Scheduler instance.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{DurableTaskSchedulerResource}" />.</returns>
    /// <example>
    /// The following example creates a preexisting Durable Task Scheduler resource configured via strings and referencing that resource in a .NET project.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var connectionString = ReferenceExpression.Create($"...");
    /// 
    /// var scheduler = builder.AddDurableTaskScheduler("scheduler")
    ///                        .RunAsExisting(connectionString);
    ///
    /// string taskHubName = "...";
    /// 
    /// var taskHub =
    ///     scheduler.AddDurableTaskHub("taskhub")
    ///              .WithTaskHubName(taskHubName);
    ///
    /// builder.AddProject&lt;Projects.MyApp&gt;("myapp")
    ///        .WithReference(taskHub);
    /// 
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<DurableTaskSchedulerResource> RunAsExisting(this IResourceBuilder<DurableTaskSchedulerResource> builder, ReferenceExpression connectionString)
    {
        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            builder.WithAnnotation(new ExistingDurableTaskSchedulerAnnotation(connectionString));

            var connectionStringParameters = ParseConnectionString(connectionString.ValueExpression);

            if (connectionStringParameters.TryGetValue("Endpoint", out string? endpoint))
            {
                builder.Resource.SchedulerEndpoint ??= new Uri(endpoint);
            }

            if (connectionStringParameters.TryGetValue("Authentication", out string? authentication))
            {
                builder.Resource.Authentication ??= authentication;
            }
        }

        return builder;
    }

    static IReadOnlyDictionary<string, string> ParseConnectionString(string connectionString)
    {
        Dictionary<string, string> dictionary = new(StringComparer.OrdinalIgnoreCase);

        var parameters = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var parameter in parameters)
        {
            var keyValue = parameter.Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (keyValue.Length != 2)
            {
                throw new ArgumentException($"Invalid connection string format: {parameter}");
            }

            dictionary[keyValue[0]] = keyValue[1];
        }

        return dictionary;
    }

    /// <summary>
    /// Adds a command to the resource that opens the Durable Task Scheduler Dashboard in a web browser.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="dashboardEndpoint"></param>
    /// <returns></returns>
    public static IResourceBuilder<DurableTaskSchedulerResource> WithDashboard(this IResourceBuilder<DurableTaskSchedulerResource> builder, string dashboardEndpoint)
    {
        return builder.WithDashboard(ReferenceExpression.Create($"{dashboardEndpoint}"));
    }

    /// <summary>
    /// Adds a command to the resource that opens the Durable Task Scheduler Dashboard in a web browser.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="dashboardEndpoint"></param>
    /// <returns></returns>
    public static IResourceBuilder<DurableTaskSchedulerResource> WithDashboard(this IResourceBuilder<DurableTaskSchedulerResource> builder, IResourceBuilder<ParameterResource>? dashboardEndpoint = null)
    {
        return builder.WithDashboard(dashboardEndpoint is not null ? ReferenceExpression.Create($"{dashboardEndpoint}") : null);
    }

    static IResourceBuilder<DurableTaskSchedulerResource> WithDashboard(this IResourceBuilder<DurableTaskSchedulerResource> builder, ReferenceExpression? dashboardEndpoint)
    {
        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            builder.WithAnnotation(new DurableTaskSchedulerDashboardAnnotation(
                subscriptionId: null,
                dashboardEndpoint: dashboardEndpoint));

            builder.WithOpenDashboardCommand(isTaskHub: false);
        }

        return builder;
    }

    /// <summary>
    /// Adds a Durable Task Scheduler task hub resource to the application model.
    /// </summary>
    /// <param name="builder">The Durable Task Scheduler resource builder.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="configure">(Optional) Callback that exposes the resource allowing for customization.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{DurableTaskHubResource}" />.</returns>
    public static IResourceBuilder<DurableTaskHubResource> AddTaskHub(this IResourceBuilder<DurableTaskSchedulerResource> builder, [ResourceName] string name, Action<IResourceBuilder<DurableTaskHubResource>>? configure = null)
    {
        DurableTaskHubResource taskHubResource = new(name, builder.Resource);

        var taskHubResourceBuilder = builder.ApplicationBuilder.AddResource(taskHubResource);

        configure?.Invoke(taskHubResourceBuilder);

        if (builder.Resource.IsEmulator)
        {
            taskHubResourceBuilder.WithDashboard();
        }

        return taskHubResourceBuilder;
    }

    /// <summary>
    /// Sets the name of the task hub if different from the resource name.
    /// </summary>
    /// <param name="builder">The Durable Task Scheduler task hub resource builder.</param>
    /// <param name="name">The name of the task hub.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{DurableTaskHubResource}" />.</returns>
    public static IResourceBuilder<DurableTaskHubResource> WithTaskHubName(this IResourceBuilder<DurableTaskHubResource> builder, string name)
    {
        builder.Resource.TaskHubName = name;

        return builder;
    }

    /// <summary>
    /// Sets the name of the task hub if different from the resource name.
    /// </summary>
    /// <param name="builder">The Durable Task Scheduler task hub resource builder.</param>
    /// <param name="name">The name of the task hub.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{DurableTaskHubResource}" />.</returns>
    public static IResourceBuilder<DurableTaskHubResource> WithTaskHubName(this IResourceBuilder<DurableTaskHubResource> builder, IResourceBuilder<ParameterResource> name)
    {
        builder.Resource.TaskHubName = name.Resource.Value;

        return builder;
    }

    /// <summary>
    /// Adds a command to the resource that opens the Durable Task Scheduler Dashboard for the task hub in a web browser.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="dashboardEndpoint"></param>
    /// <param name="subscriptionId"></param>
    /// <returns></returns>
    public static IResourceBuilder<DurableTaskHubResource> WithDashboard(this IResourceBuilder<DurableTaskHubResource> builder, string? dashboardEndpoint, string? subscriptionId)
    {
        return builder.WithDashboard(
            dashboardEndpoint: dashboardEndpoint is not null ? ReferenceExpression.Create($"{dashboardEndpoint}") : null,
            subscriptionId: subscriptionId is not null ? ReferenceExpression.Create($"{subscriptionId}") : null);
    }

    /// <summary>
    /// Adds a command to the resource that opens the Durable Task Scheduler Dashboard for the task hub in a web browser.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="dashboardEndpoint"></param>
    /// <param name="subscriptionId"></param>
    /// <returns></returns>
    public static IResourceBuilder<DurableTaskHubResource> WithDashboard(this IResourceBuilder<DurableTaskHubResource> builder, IResourceBuilder<ParameterResource>? dashboardEndpoint = null, IResourceBuilder<ParameterResource>? subscriptionId = null)
    {
        return builder.WithDashboard(
            dashboardEndpoint: dashboardEndpoint is not null ? ReferenceExpression.Create($"{dashboardEndpoint}") : null,
            subscriptionId: subscriptionId is not null ? ReferenceExpression.Create($"{subscriptionId}") : null);
    }

    static IResourceBuilder<DurableTaskHubResource> WithDashboard(this IResourceBuilder<DurableTaskHubResource> builder, ReferenceExpression? dashboardEndpoint, ReferenceExpression? subscriptionId)
    {
        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            builder.WithAnnotation(new DurableTaskSchedulerDashboardAnnotation(
                subscriptionId: subscriptionId,
                dashboardEndpoint: dashboardEndpoint));

            builder.WithOpenDashboardCommand(isTaskHub: true);
        }

        return builder;
    }

    /// <summary>
    /// Enables the use of dynamic task hubs for the emulator.
    /// </summary>
    /// <param name="builder">The Durable Task Scheduler emulator resource builder.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{DurableTaskSchedulerEmulatorResource}" />.</returns>
    /// <remarks>
    /// Using dynamic task hubs eliminates the requirement that they be pre-defined,
    /// which can be useful when the same emulator instance is used across sessions.
    /// </remarks>
    public static IResourceBuilder<DurableTaskSchedulerEmulatorResource> WithDynamicTaskHubs(this IResourceBuilder<DurableTaskSchedulerEmulatorResource> builder)
    {
        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            builder.Resource.UseDynamicTaskHubs = true;
        }

        return builder;
    }

    static IResourceBuilder<T> WithOpenDashboardCommand<T>(this IResourceBuilder<T> builder, bool isTaskHub) where T : IDurableTaskResourceWithDashboard
    {
        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            builder.WithCommand(
                isTaskHub ? "durabletask-hub-open-dashboard" : "durabletask-scheduler-open-dashboard",
                "Open Dashboard",
                async context =>
                {
                    var dashboardEndpoint = await builder.Resource.DashboardEndpointExpression.GetValueAsync(context.CancellationToken).ConfigureAwait(false);

                    Process.Start(new ProcessStartInfo { FileName = dashboardEndpoint, UseShellExecute = true });

                    return CommandResults.Success();
                },
                new()
                {
                    Description = "Open the Durable Task Scheduler Dashboard",
                    IconName = "GlobeArrowForward",
                    IsHighlighted = isTaskHub,
                });
        }

        return builder;
    }
}
