// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Reflection;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting.Testing;

/// <summary>
/// Methods for creating distributed application instances for testing purposes.
/// </summary>
public static class DistributedApplicationTestingBuilder
{
    /// <summary>
    /// Creates a new instance of <see cref="DistributedApplicationTestingBuilder"/>.
    /// </summary>
    /// <typeparam name="TEntryPoint">
    /// A type in the entry point assembly of the target Aspire AppHost. Typically, the Program class can be used.
    /// </typeparam>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>
    /// A new instance of <see cref="DistributedApplicationTestingBuilder"/>.
    /// </returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Generic and non-generic")]
    public static Task<IDistributedApplicationTestingBuilder> CreateAsync<TEntryPoint>(CancellationToken cancellationToken = default)
        where TEntryPoint : class
        => CreateAsync(typeof(TEntryPoint), cancellationToken);

    /// <summary>
    /// Creates a new instance of <see cref="DistributedApplicationTestingBuilder"/>.
    /// </summary>
    /// <param name="entryPoint">A type in the entry point assembly of the target Aspire AppHost. Typically, the Program class can be used.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>
    /// A new instance of <see cref="DistributedApplicationTestingBuilder"/>.
    /// </returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Generic and non-generic")]
    public static Task<IDistributedApplicationTestingBuilder> CreateAsync(Type entryPoint, CancellationToken cancellationToken = default)
        => CreateAsync(entryPoint, [], cancellationToken);

    /// <summary>
    /// Creates a new instance of <see cref="DistributedApplicationTestingBuilder"/>.
    /// </summary>
    /// <typeparam name="TEntryPoint">
    /// A type in the entry point assembly of the target Aspire AppHost. Typically, the Program class can be used.
    /// </typeparam>
    /// <param name="args">The command line arguments to pass to the entry point.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>
    /// A new instance of <see cref="DistributedApplicationTestingBuilder"/>.
    /// </returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Generic and non-generic")]
    public static Task<IDistributedApplicationTestingBuilder> CreateAsync<TEntryPoint>(string[] args, CancellationToken cancellationToken = default)
        where TEntryPoint : class
        => CreateAsync(typeof(TEntryPoint), args, (_, __) => { }, cancellationToken);

    /// <summary>
    /// Creates a new instance of <see cref="DistributedApplicationTestingBuilder"/>.
    /// </summary>
    /// <param name="entryPoint">A type in the entry point assembly of the target Aspire AppHost. Typically, the Program class can be used.</param>
    /// <param name="args">The command line arguments to pass to the entry point.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>
    /// A new instance of <see cref="DistributedApplicationTestingBuilder"/>.
    /// </returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Generic and non-generic")]
    public static Task<IDistributedApplicationTestingBuilder> CreateAsync(Type entryPoint, string[] args, CancellationToken cancellationToken = default)
        => CreateAsync(entryPoint, args, (_, __) => { }, cancellationToken);

    /// <summary>
    /// Creates a new instance of <see cref="DistributedApplicationTestingBuilder"/>.
    /// </summary>
    /// <typeparam name="TEntryPoint">
    /// A type in the entry point assembly of the target Aspire AppHost. Typically, the Program class can be used.
    /// </typeparam>
    /// <param name="args">The command line arguments to pass to the entry point.</param>
    /// <param name="configureBuilder">The delegate used to configure the builder.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>
    /// A new instance of <see cref="DistributedApplicationTestingBuilder"/>.
    /// </returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Generic and non-generic")]
    public static Task<IDistributedApplicationTestingBuilder> CreateAsync<TEntryPoint>(string[] args, Action<DistributedApplicationOptions, HostApplicationBuilderSettings> configureBuilder, CancellationToken cancellationToken = default)
        => CreateAsync(typeof(TEntryPoint), args, configureBuilder, cancellationToken);

    /// <summary>
    /// Creates a new instance of <see cref="DistributedApplicationTestingBuilder"/>.
    /// </summary>
    /// <param name="entryPoint">A type in the entry point assembly of the target Aspire AppHost. Typically, the Program class can be used.</param>
    /// <param name="args">The command line arguments to pass to the entry point.</param>
    /// <param name="configureBuilder">The delegate used to configure the builder.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>
    /// A new instance of <see cref="DistributedApplicationTestingBuilder"/>.
    /// </returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Generic and non-generic")]
    public static async Task<IDistributedApplicationTestingBuilder> CreateAsync(Type entryPoint, string[] args, Action<DistributedApplicationOptions, HostApplicationBuilderSettings> configureBuilder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entryPoint, nameof(entryPoint));
        ArgumentNullException.ThrowIfNull(args, nameof(args));
        ArgumentNullException.ThrowIfNull(configureBuilder, nameof(configureBuilder));

        var factory = new SuspendingDistributedApplicationFactory(entryPoint, args, configureBuilder);
        return await factory.CreateBuilderAsync(cancellationToken).ConfigureAwait(false);
    }

    private sealed class SuspendingDistributedApplicationFactory(Type entryPoint, string[] args, Action<DistributedApplicationOptions, HostApplicationBuilderSettings> configureBuilder)
        : DistributedApplicationFactory(entryPoint, args)
    {
        private readonly SemaphoreSlim _continueBuilding = new(0);

        public async Task<IDistributedApplicationTestingBuilder> CreateBuilderAsync(CancellationToken cancellationToken)
        {
            var innerBuilder = await ResolveBuilderAsync(cancellationToken).ConfigureAwait(false);
            return new Builder(this, innerBuilder);
        }

        protected override void OnBuilderCreating(DistributedApplicationOptions applicationOptions, HostApplicationBuilderSettings hostOptions)
        {
            base.OnBuilderCreating(applicationOptions, hostOptions);
            configureBuilder(applicationOptions, hostOptions);
        }

        protected override void OnBuilderCreated(DistributedApplicationBuilder applicationBuilder)
        {
            base.OnBuilderCreated(applicationBuilder);
        }

        protected override void OnBuilding(DistributedApplicationBuilder applicationBuilder)
        {
            base.OnBuilding(applicationBuilder);

            // Wait until the owner signals that building can continue by calling BuildAsync().
            _continueBuilding.Wait();
        }

        public async Task<DistributedApplication> BuildAsync(CancellationToken cancellationToken)
        {
            _continueBuilding.Release();
            return await ResolveApplicationAsync(cancellationToken).ConfigureAwait(false);
        }

        public override async ValueTask DisposeAsync()
        {
            _continueBuilding.Release();
            await base.DisposeAsync().ConfigureAwait(false);
        }

        public override void Dispose()
        {
            _continueBuilding.Release();
            base.Dispose();
        }

        private sealed class Builder(SuspendingDistributedApplicationFactory factory, DistributedApplicationBuilder innerBuilder) : IDistributedApplicationTestingBuilder
        {
            public ConfigurationManager Configuration => innerBuilder.Configuration;

            public string AppHostDirectory => innerBuilder.AppHostDirectory;

            public Assembly? AppHostAssembly => innerBuilder.AppHostAssembly;

            public IHostEnvironment Environment => innerBuilder.Environment;

            public IServiceCollection Services => innerBuilder.Services;

            public DistributedApplicationExecutionContext ExecutionContext => innerBuilder.ExecutionContext;

            public IResourceCollection Resources => innerBuilder.Resources;

            public IDistributedApplicationEventing Eventing => innerBuilder.Eventing;

            public IResourceBuilder<T> AddResource<T>(T resource) where T : IResource => innerBuilder.AddResource(resource);

            public async Task<DistributedApplication> BuildAsync(CancellationToken cancellationToken)
            {
                var innerApp = await factory.BuildAsync(cancellationToken).ConfigureAwait(false);
                return new DelegatedDistributedApplication(new DelegatedHost(factory, innerApp));
            }

            public IResourceBuilder<T> CreateResourceBuilder<T>(T resource) where T : IResource => innerBuilder.CreateResourceBuilder(resource);
        }

        private sealed class DelegatedDistributedApplication(DelegatedHost host) : DistributedApplication(host)
        {
            private readonly DelegatedHost _host = host;

            public override async Task RunAsync(CancellationToken cancellationToken)
            {
                // Avoid calling the base here, since it will execute the pre-start hooks
                // before calling the corresponding host method, which also executes the same pre-start hooks.
                await _host.RunAsync(cancellationToken).ConfigureAwait(false);
            }

            public override async Task StartAsync(CancellationToken cancellationToken)
            {
                // Avoid calling the base here, since it will execute the pre-start hooks
                // before calling the corresponding host method, which also executes the same pre-start hooks.
                await _host.StartAsync(cancellationToken).ConfigureAwait(false);
            }

            public override async Task StopAsync(CancellationToken cancellationToken)
            {
                await _host.StopAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private sealed class DelegatedHost(SuspendingDistributedApplicationFactory appFactory, DistributedApplication innerApp) : IHost, IAsyncDisposable
        {
            public IServiceProvider Services => innerApp.Services;

            public void Dispose()
            {
                appFactory.Dispose();
            }

            public async ValueTask DisposeAsync()
            {
                await appFactory.DisposeAsync().ConfigureAwait(false);
            }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                await appFactory.StartAsync(cancellationToken).ConfigureAwait(false);
            }

            public async Task StopAsync(CancellationToken cancellationToken)
            {
                await appFactory.DisposeAsync().AsTask().WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}

/// <summary>
/// A builder for creating instances of <see cref="DistributedApplication"/> for testing purposes.
/// </summary>
public interface IDistributedApplicationTestingBuilder
{
    /// <inheritdoc cref="HostApplicationBuilder.Configuration" />
    ConfigurationManager Configuration { get; }

    /// <summary>
    /// Directory of the project where the app host is located. Defaults to the content root if there's no project.
    /// </summary>
    string AppHostDirectory { get; }

    /// <summary>
    /// The assembly of the app host.
    /// </summary>
    Assembly? AppHostAssembly { get; }

    /// <inheritdoc cref="HostApplicationBuilder.Environment" />
    IHostEnvironment Environment { get; }

    /// <inheritdoc cref="HostApplicationBuilder.Services" />
    IServiceCollection Services { get; }

    /// <summary>
    /// Execution context for this invocation of the AppHost.
    /// </summary>
    DistributedApplicationExecutionContext ExecutionContext { get; }

    /// <summary>
    /// Eventing infrastructure for AppHost lifecycle.
    /// </summary>
    IDistributedApplicationEventing Eventing { get; }

    /// <summary>
    /// Gets the collection of resources for the distributed application.
    /// </summary>
    /// <remarks>
    /// This can be mutated by adding more resources, which will update its current view.
    /// </remarks>
    IResourceCollection Resources { get; }

    /// <summary>
    /// Adds a resource of type <typeparamref name="T"/> to the distributed application.
    /// </summary>
    /// <typeparam name="T">The type of resource to add.</typeparam>
    /// <param name="resource">The resource to add.</param>
    /// <returns>A innerBuilder for configuring the added resource.</returns>
    /// <exception cref="DistributedApplicationException">Thrown when a resource with the same name already exists.</exception>
    IResourceBuilder<T> AddResource<T>(T resource) where T : IResource;

    /// <summary>
    /// Creates a new resource innerBuilder based on an existing resource.
    /// </summary>
    /// <typeparam name="T">Type of resource.</typeparam>
    /// <param name="resource">An existing resource.</param>
    /// <returns>A resource innerBuilder.</returns>
    IResourceBuilder<T> CreateResourceBuilder<T>(T resource) where T : IResource;

    /// <summary>
    /// Builds and returns a new <see cref="DistributedApplication"/> instance. This can only be called once.
    /// </summary>
    /// <returns>A new <see cref="DistributedApplication"/> instance.</returns>
    Task<DistributedApplication> BuildAsync(CancellationToken cancellationToken = default);
}
