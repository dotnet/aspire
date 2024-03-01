// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting.Testing;

/// <summary>
/// Harness for running a distributed application for testing.
/// </summary>
/// <typeparam name="TEntryPoint">
/// A type in the entry point assembly of the target Aspire AppHost. Typically, the Program class can be used.
/// </typeparam>
public sealed class DistributedApplicationTestingBuilder<TEntryPoint> : IDistributedApplicationBuilder where TEntryPoint : class
{
    private readonly SuspendingDistributedApplicationFactory _factory;
    private readonly DistributedApplicationBuilder _builder;

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedApplicationTestingBuilder{TEntryPoint}"/> class.
    /// </summary>
    public DistributedApplicationTestingBuilder() : this((_, __) => { })
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedApplicationTestingBuilder{TEntryPoint}"/> class.
    /// </summary>
    /// <param name="configureBuilder">The delegate used to configure the creation of the builder.</param>
    public DistributedApplicationTestingBuilder(Action<DistributedApplicationOptions, HostApplicationBuilderSettings> configureBuilder)
    {
        _factory = new(configureBuilder);
        _builder = _factory.DistributedApplicationBuilder;
    }

    /// <inheritdoc cref="IDistributedApplicationBuilder.Configuration" />
    public ConfigurationManager Configuration => _builder.Configuration;

    /// <inheritdoc cref="IDistributedApplicationBuilder.Environment" />
    public string AppHostDirectory => _builder.AppHostDirectory;

    /// <inheritdoc cref="HostApplicationBuilder.Environment" />
    public IHostEnvironment Environment => _builder.Environment;

    /// <inheritdoc cref="IDistributedApplicationBuilder.Services" />
    public IServiceCollection Services => _builder.Services;

    /// <inheritdoc cref="IDistributedApplicationBuilder.ExecutionContext" />
    public DistributedApplicationExecutionContext ExecutionContext => _builder.ExecutionContext;

    /// <inheritdoc cref="IDistributedApplicationBuilder.Resources" />
    public IResourceCollection Resources => _builder.Resources;

    /// <inheritdoc cref="IDistributedApplicationBuilder.AddResource{T}(T)" />
    public IResourceBuilder<T> AddResource<T>(T resource) where T : IResource => _builder.AddResource(resource);

    /// <inheritdoc cref="IDistributedApplicationBuilder.CreateResourceBuilder{T}(T)" />
    public IResourceBuilder<T> CreateResourceBuilder<T>(T resource) where T : IResource => _builder.CreateResourceBuilder(resource);

    /// <inheritdoc cref="IDistributedApplicationBuilder.Build" />
    public DistributedApplication Build()
    {
        _factory.Build();
        return new DelegatedDistributedApplication(new DelegatedHost(_factory));
    }

    private sealed class SuspendingDistributedApplicationFactory(Action<DistributedApplicationOptions, HostApplicationBuilderSettings> configureBuilder)
        : DistributedApplicationTestingHarness<TEntryPoint>
    {
        private readonly SemaphoreSlim _continueBuilding = new(0);

        public new DistributedApplication DistributedApplication => base.DistributedApplication;

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

            // Wait until the owner signals that building can continue by calling Build().
            _continueBuilding.Wait();
        }

        public void Build()
        {
            _continueBuilding.Release();
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

    private sealed class DelegatedHost(SuspendingDistributedApplicationFactory appFactory) : IHost, IAsyncDisposable
    {
        public IServiceProvider Services => appFactory.Services;

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
            await appFactory.InitializeAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await appFactory.DisposeAsync().ConfigureAwait(false);
        }
    }
}
