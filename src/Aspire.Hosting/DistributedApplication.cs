// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

/// <summary>
/// Represents a distributed application that implements the <see cref="IHost"/> and <see cref="IAsyncDisposable"/> interfaces.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="DistributedApplication"/> is an implementation of the <see cref="IHost"/> interface that orchestrates
/// a .NET Aspire application. To build an instance of the <see cref="DistributedApplication"/> class, use the
/// <see cref="DistributedApplication.CreateBuilder()"/> method to create an instance of the <see cref="IDistributedApplicationBuilder"/>
/// interface. Using the <see cref="IDistributedApplicationBuilder"/> interface you can configure the resources
/// that comprise the distributed application and describe the dependencies between them.
/// </para>
/// <para>
/// Once the distributed application has been defined use the <see cref="IDistributedApplicationBuilder.Build()"/> method
/// to create an instance of the <see cref="DistributedApplication"/> class. The <see cref="DistributedApplication"/> class
/// exposes a <see cref="DistributedApplication.Run"/> method which then starts the distributed application and its
/// resources.
/// </para>
/// <para>
/// The <see cref="CreateBuilder(Aspire.Hosting.DistributedApplicationOptions)"/> method provides additional options for
/// constructing the <see cref="IDistributedApplicationBuilder"/> including disabling the .NET Aspire dashboard (see <see cref="DistributedApplicationOptions.DisableDashboard"/>) or
/// allowing unsecured communication between the browser and dashboard, and dashboard and app host (see <see cref="DistributedApplicationOptions.AllowUnsecuredTransport"/>.
/// </para>
/// </remarks>
/// <example>
/// The following example shows creating a PostgreSQL server resource with a database and referencing that
/// database in a .NET project.
/// <code lang="csharp">
/// var builder = DistributedApplication.CreateBuilder(args);
/// var inventoryDatabase = builder.AddPostgres("mypostgres").AddDatabase("inventory");
/// builder.AddProject&lt;Projects.InventoryService&gt;()
///        .WithReference(inventoryDatabase);
///
/// builder.Build().Run();
/// </code>
/// </example>
[DebuggerDisplay("{_host}")]
public class DistributedApplication : IHost, IAsyncDisposable
{
    private readonly IHost _host;
    private ResourceNotificationService? _resourceNotifications;

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedApplication"/> class.
    /// </summary>
    /// <param name="host">The <see cref="IHost"/> instance.</param>
    public DistributedApplication(IHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        _host = host;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="IDistributedApplicationBuilder"/> interface.
    /// </summary>
    /// <returns>A new instance of the <see cref="IDistributedApplicationBuilder"/> interface.</returns>
    /// <remarks>
    /// This overload of the <see cref="CreateBuilder()"/> method should only be
    /// used when the app host is not intended to be used with a deployment tool. Because no arguments are
    /// passed to the <see cref="CreateBuilder()"/> method the app host has no
    /// way to be put into publish mode. Refer to <see cref="CreateBuilder(string[])"/> or <see cref="CreateBuilder(DistributedApplicationOptions)"/>
    /// when more control is needed over the behavior of the distributed application at runtime.
    /// </remarks>
    /// <example>
    /// The following example is creating a Postgres server resource with a database and referencing that
    /// database in a .NET project.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder();
    /// var inventoryDatabase = builder.AddPostgres("mypostgres").AddDatabase("inventory");
    /// builder.AddProject&lt;Projects.InventoryService&gt;()
    ///        .WithReference(inventoryDatabase);
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IDistributedApplicationBuilder CreateBuilder() => CreateBuilder([]);

    /// <summary>
    /// Creates a new instance of <see cref="IDistributedApplicationBuilder"/> with the specified command-line arguments.
    /// </summary>
    /// <param name="args">The command-line arguments to use when building the distributed application.</param>
    /// <returns>A new instance of <see cref="IDistributedApplicationBuilder"/>.</returns>
    /// <remarks>
    /// <para>
    /// The <see cref="DistributedApplication.CreateBuilder(string[])"/> method is the most common way to
    /// create an instance of the <see cref="IDistributedApplicationBuilder"/> interface. Typically this
    /// method will be called as a top-level statement in the application's entry-point.
    /// </para>
    /// <para>
    /// Note that the <paramref name="args"/> parameter is a <see langword="string"/> and is essential in allowing the application
    /// host to work with deployment tools because arguments are used to tell the application host that it
    /// is in publish mode. If <paramref name="args"/> is not provided the application will not work with
    /// deployment tools. It is also possible to provide arguments using the <see cref="CreateBuilder(Aspire.Hosting.DistributedApplicationOptions)"/>
    /// overload of this method.
    /// </para>
    /// <example>
    /// The following example shows creating a Postgres server resource with a database and referencing that
    /// database in a .NET project.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// var inventoryDatabase = builder.AddPostgres("mypostgres").AddDatabase("inventory");
    /// builder.AddProject&lt;Projects.InventoryService&gt;()
    ///        .WithReference(inventoryDatabase);
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    /// <example>
    /// The following example is equivalent to the previous example except that it does not use top-level statements.
    /// <code lang="csharp">
    /// public class Program
    /// {
    ///     public static void Main(string[] args)
    ///     {
    ///         var builder = DistributedApplication.CreateBuilder(args);
    ///         var inventoryDatabase = builder.AddPostgres("mypostgres").AddDatabase("inventory");
    ///         builder.AddProject&lt;Projects.InventoryService&gt;()
    ///                .WithReference(inventoryDatabase);
    ///
    ///         builder.Build().Run();
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public static IDistributedApplicationBuilder CreateBuilder(string[] args)
    {
        WaitForDebugger();

        ArgumentNullException.ThrowIfNull(args);

        var builder = new DistributedApplicationBuilder(new DistributedApplicationOptions() { Args = args });
        return builder;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="IDistributedApplicationBuilder"/> interface with the specified <paramref name="options"/>.
    /// </summary>
    /// <param name="options">The <see cref="DistributedApplicationOptions"/> to use for configuring the builder.</param>
    /// <returns>A new instance of the <see cref="IDistributedApplicationBuilder"/> interface.</returns>
    /// <remarks>
    /// <para>
    /// The <see cref="DistributedApplication.CreateBuilder(DistributedApplicationOptions)"/> method provides
    /// greater control over the behavior of the distributed application at runtime. For example providing
    /// an <paramref name="options"/> argument allows developers to force all container images to be loaded
    /// from a specified container registry by using the <see cref="DistributedApplicationOptions.ContainerRegistryOverride"/>
    /// property, or disabling the dashboard by using the <see cref="DistributedApplicationOptions.DisableDashboard"/>
    /// property. Refer to the <see cref="DistributedApplicationOptions"/> class for more details on
    /// each option that may be provided.
    /// </para>
    /// <para>
    /// When supplying a custom <see cref="DistributedApplicationOptions"/> it is recommended to populate the
    /// <see cref="DistributedApplicationOptions.Args"/> property to ensure that the app host continues to function
    /// correctly when used with deployment tools that need to enable publish mode.
    /// </para>
    /// </remarks>
    /// <example>
    /// Override the container registry used by the distributed application.
    /// <code lang="csharp">
    /// var options = new DistributedApplicationOptions
    /// {
    ///     Args = args; // Important for deployment tools
    ///     ContainerRegistryOverride = "registry.example.com"
    /// };
    /// var builder = DistributedApplication.CreateBuilder(options);
    /// var inventoryDatabase = builder.AddPostgres("mypostgres").AddDatabase("inventory");
    /// builder.AddProject&lt;Projects.InventoryService&gt;()
    ///        .WithReference(inventoryDatabase);
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IDistributedApplicationBuilder CreateBuilder(DistributedApplicationOptions options)
    {
        WaitForDebugger();

        ArgumentNullException.ThrowIfNull(options);

        var builder = new DistributedApplicationBuilder(options);
        return builder;
    }

    private static void WaitForDebugger()
    {
        if (Environment.GetEnvironmentVariable(KnownConfigNames.WaitForDebugger) == "true")
        {
            var startedWaiting = DateTimeOffset.UtcNow;
            TimeSpan timeout = TimeSpan.FromSeconds(30);

            if (Environment.GetEnvironmentVariable(KnownConfigNames.WaitForDebuggerTimeout) is string timeoutString && int.TryParse(timeoutString, out var timeoutSeconds))
            {
                timeout = TimeSpan.FromSeconds(timeoutSeconds);
            }

            while (Debugger.IsAttached == false)
            {
                Console.WriteLine($"Waiting for debugger to attach to process: {Environment.ProcessId}");

                if (DateTimeOffset.UtcNow - startedWaiting > timeout)
                {
                    Console.WriteLine($"Timeout waiting for debugger to attach to process: {Environment.ProcessId}");
                    break;
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }
    }

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance configured for the application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="DistributedApplication"/> is an <see cref="IHost"/> implementation and as such
    /// exposes a <see cref="Services"/> property which allows developers to get services from the
    /// dependency injection container after <see cref="DistributedApplication" /> instance has been
    /// built using the <see cref="IDistributedApplicationBuilder.Build"/> method.
    /// </para>
    /// <para>
    /// To add services to the dependency injection container developers should use the <see cref="IDistributedApplicationBuilder.Services"/>
    /// property to access the <see cref="IServiceCollection"/> instance.
    /// </para>
    /// </remarks>
    public IServiceProvider Services => _host.Services;

    /// <summary>
    /// Gets the service for monitoring and responding to resource state changes in the distributed application.
    /// </summary>
    /// <remarks>
    /// Two common use cases for the <see cref="ResourceNotificationService"/> are:
    /// <list type="bullet">
    /// <item>Database seeding.</item>
    /// <item>Integration test readiness checks.</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// Wait for resource readiness:
    /// <code>
    /// await app.ResourceNotifications.WaitForResourceHealthyAsync("postgres");
    /// </code>
    /// </example>
    /// <example>
    /// Monitor state changes:
    /// <code>
    /// await foreach (var update in app.ResourceNotifications.WatchAsync(cancellationToken))
    /// {
    ///     Console.WriteLine($"Resource {update.Resource.Name} state: {update.Snapshot.State?.Text}");
    /// }
    /// </code>
    /// </example>
    /// <example>
    /// Wait for a specific state:
    /// <code>
    /// await app.ResourceNotifications.WaitForResourceAsync("worker", KnownResourceStates.Running);
    /// </code>
    /// </example>
    /// <example>
    /// Seed a database once it becomes available:
    /// <code>
    /// // Wait for the database to be healthy before seeding
    /// await app.ResourceNotifications.WaitForResourceHealthyAsync("postgres");
    /// using var scope = app.Services.CreateScope();
    /// var dbContext = scope.ServiceProvider.GetRequiredService&lt;ApplicationDbContext&gt;();
    /// await dbContext.Database.EnsureCreatedAsync();
    /// if (!dbContext.Products.Any())
    /// {
    ///     await dbContext.Products.AddRangeAsync(
    ///     [
    ///         new Product { Name = "Product 1", Price = 10.99m },
    ///         new Product { Name = "Product 2", Price = 20.99m }
    ///     ]);
    ///     await dbContext.SaveChangesAsync();
    /// }
    /// </code>
    /// </example>
    public ResourceNotificationService ResourceNotifications => _resourceNotifications ??= _host.Services.GetRequiredService<ResourceNotificationService>();

    /// <summary>
    /// Disposes the distributed application by disposing the <see cref="IHost"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Typically developers do not need to worry about calling the Dispose method on the <see cref="DistributedApplication"/>
    /// instance because it is typically used in the entry point of the application and all resources
    /// used by the application are destroyed when the application exists.
    /// </para>
    /// <para>
    /// If you are using the <see cref="DistributedApplication"/> and <see cref="IDistributedApplicationBuilder"/> inside
    /// unit test code then you should correctly dispose of the <see cref="DistributedApplication"/> instance. This is
    /// because the <see cref="IDistributedApplicationBuilder" /> instance initializes configuration providers which
    /// make use of file watchers which are a finite resource.
    /// </para>
    /// <para>
    /// Without disposing of the <see cref="DistributedApplication"/>
    /// correctly projects with a large number of functional/integration tests may see a "The configured user limit (128) on
    /// the number of inotify instances has been reached, or the per-process limit on the number of open file descriptors
    /// has been reached." error.
    /// </para>
    /// <para>
    /// Refer to the <see href="https://aka.ms/dotnet/aspire/testing" >.NET Aspire testing page</see> for more information
    /// on how to use .NET Aspire APIs for functional an integrating testing.
    /// </para>
    /// </remarks>
    public virtual void Dispose()
    {
        _host.Dispose();
    }

    /// <summary>
    /// Asynchronously disposes the distributed application by disposing the <see cref="IHost"/>.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// Typically developers do not need to worry about calling the Dispose method on the <see cref="DistributedApplication"/>
    /// instance because it is typically used in the entry point of the application and all resources
    /// used by the application are destroyed when the application exists.
    /// </para>
    /// <para>
    /// If you are using the <see cref="DistributedApplication"/> and <see cref="IDistributedApplicationBuilder"/> inside
    /// unit test code then you should correctly dispose of the <see cref="DistributedApplication"/> instance. This is
    /// because the <see cref="IDistributedApplicationBuilder" /> instance initializes configuration providers which
    /// make use of file watchers which are a finite resource.
    /// </para>
    /// <para>
    /// Without disposing of the <see cref="DistributedApplication"/>
    /// correctly projects with a large number of functional/integration tests may see a "The configured user limit (128) on
    /// the number of inotify instances has been reached, or the per-process limit on the number of open file descriptors
    /// has been reached." error.
    /// </para>
    /// <para>
    /// Refer to the <see href="https://aka.ms/dotnet/aspire/testing" >.NET Aspire testing page</see> for more information
    /// on how to use .NET Aspire APIs for functional an integrating testing.
    /// </para>
    /// </remarks>
    public virtual ValueTask DisposeAsync()
    {
        return ((IAsyncDisposable)_host).DisposeAsync();
    }

    /// <inheritdoc cref="IHost.StartAsync" />
    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        // We only run the start lifecycle hook if we are in run mode or
        // publish mode. In inspect mode we try to avoid lifecycle hooks
        // kickings. Eventing will still work generally since they are more
        // targetted.
        var executionContext = _host.Services.GetRequiredService<DistributedApplicationExecutionContext>();
        if (executionContext.IsPublishMode || executionContext.IsRunMode)
        {
            await ExecuteBeforeStartHooksAsync(cancellationToken).ConfigureAwait(false);
        }

        await _host.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc cref="IHost.StopAsync" />
    public virtual async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _host.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs an application and returns a Task that only completes when the token is triggered or shutdown is
    /// triggered and all <see cref="IHostedService" /> instances are stopped.
    /// </summary>
    /// <param name="cancellationToken">The token to trigger shutdown.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// When the .NET Aspire app host is launched via <see cref="DistributedApplication.RunAsync"/> there are
    /// two possible modes that it is running in:
    /// </para>
    /// <list type="number">
    /// <item>Run mode; in run mode the app host runs until a shutdown of the app is triggered
    /// either by the users pressing <c>Ctrl-C</c>, the debugger detaching, or the browser associated
    /// with the dashboard being closed.</item>
    /// <item>Publish mode; in publish mode the app host runs just long enough to generate a
    /// manifest file that is used by deployment tool.</item>
    /// </list>
    /// <para>
    /// Developers extending the .NET Aspire application model should consider the lifetime
    /// of <see cref="IHostedService"/> instances which are added to the dependency injection
    /// container. For more information on determining the mode that the app host is running
    /// in refer to <see cref="DistributedApplicationExecutionContext" />.
    /// </para>
    /// </remarks>
    public virtual async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // We only run the start lifecycle hook if we are in run mode or
        // publish mode. In inspect mode we try to avoid lifecycle hooks
        // kickings. Eventing will still work generally since they are more
        // targetted.
        var executionContext = _host.Services.GetRequiredService<DistributedApplicationExecutionContext>();
        if (executionContext.IsPublishMode || executionContext.IsRunMode)
        {
            await ExecuteBeforeStartHooksAsync(cancellationToken).ConfigureAwait(false);
        }

        await _host.RunAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs an application and blocks the calling thread until host shutdown is triggered and all
    /// <see cref="IHostedService"/> instances are stopped.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When the .NET Aspire app host is launched via <see cref="DistributedApplication.RunAsync"/> there are
    /// two possible modes that it is running in:
    /// </para>
    /// <list type="number">
    /// <item>Run mode; in run mode the app host runs until a shutdown of the app is triggered
    /// either by the users pressing <c>Ctrl-C</c>, the debugger detaching, or the browser associated
    /// with the dashboard being closed.</item>
    /// <item>Publish mode; in publish mode the app host runs just long enough to generate a
    /// manifest file that is used by deployment tool.</item>
    /// </list>
    /// <para>
    /// Developers extending the .NET Aspire application model should consider the lifetime
    /// of <see cref="IHostedService"/> instances which are added to the dependency injection
    /// container. For more information on determining the mode that the app host is running
    /// in refer to <see cref="DistributedApplicationExecutionContext" />.
    /// </para>
    /// </remarks>
    public void Run()
    {
        RunAsync().Wait();
    }

    // Internal for testing
    internal async Task ExecuteBeforeStartHooksAsync(CancellationToken cancellationToken)
    {
        AspireEventSource.Instance.AppBeforeStartHooksStart();

        try
        {
            var beforeStartEvent = new BeforeStartEvent(_host.Services, _host.Services.GetRequiredService<DistributedApplicationModel>());
            var eventing = _host.Services.GetRequiredService<IDistributedApplicationEventing>();
            await eventing.PublishAsync(beforeStartEvent, cancellationToken).ConfigureAwait(false);

            var lifecycleHooks = _host.Services.GetServices<IDistributedApplicationLifecycleHook>();
            var appModel = _host.Services.GetRequiredService<DistributedApplicationModel>();

            foreach (var lifecycleHook in lifecycleHooks)
            {
                await lifecycleHook.BeforeStartAsync(appModel, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            AspireEventSource.Instance.AppBeforeStartHooksStop();
        }
    }

    Task IHost.StartAsync(CancellationToken cancellationToken) => StartAsync(cancellationToken);

    Task IHost.StopAsync(CancellationToken cancellationToken) => StopAsync(cancellationToken);
}
