// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Testing;

/// <summary>
/// Harness for running a distributed application for testing.
/// </summary>
/// <typeparam name="TEntryPoint">
/// A type in the entry point assembly of the target Aspire AppHost. Typically, the Program class can be used.
/// </typeparam>
public class DistributedApplicationTestingHarness<TEntryPoint> : IDisposable, IAsyncDisposable where TEntryPoint : class
{
    private readonly TaskCompletionSource _startedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _exitTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly object _lockObj = new();
    private Task<DistributedApplication>? _appTask;
    private DistributedApplication? _app;
    private IHostApplicationLifetime? _hostApplicationLifetime;

    /// <summary>
    /// Gets the distributed application associated with this <see cref="DistributedApplicationTestingHarness{TEntryPoint}"/>.
    /// </summary>
    protected DistributedApplication DistributedApplication { get { EnsureApp(); return _app; } }

    /// <summary>
    /// Initializes the application.
    /// </summary>
    /// <param name="cancellationToken">A token used to signal cancellation.</param>
    /// <returns>A <see cref="Task"/> representing the completion of the operation.</returns>
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        EnsureApp();
        return _startedTcs.Task.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> created by the server associated with this <see cref="DistributedApplicationTestingHarness{TEntryPoint}"/>.
    /// </summary>
    public virtual IServiceProvider Services
    {
        get
        {
            EnsureApp();
            return _app.Services;
        }
    }

    /// <summary>
    /// Gets the <see cref="DistributedApplicationModel"/> associated with this <see cref="DistributedApplicationTestingHarness{TEntryPoint}"/>.
    /// </summary>
    public DistributedApplicationModel ApplicationModel
    {
        get
        {
            EnsureApp();
            ThrowIfNotInitialized();
            return _app.Services.GetRequiredService<DistributedApplicationModel>();
        }
    }

    /// <summary>
    /// Creates an instance of <see cref="HttpClient"/> that is configured to route requests to the specified resource and endpoint.
    /// </summary>
    /// <returns>The <see cref="HttpClient"/>.</returns>
    public HttpClient CreateHttpClient(string resourceName, string? endpointName = default)
    {
        EnsureApp();
        ThrowIfNotInitialized();

        return DistributedApplication.CreateHttpClient(resourceName, endpointName);
    }

    /// <summary>
    /// Gets the connection string for the specified resource.
    /// </summary>
    /// <param name="resourceName">The resource name.</param>
    /// <returns>The connection string for the specified resource.</returns>
    /// <exception cref="ArgumentException">The resource was not found or does not expose a connection string.</exception>
    public string? GetConnectionString(string resourceName)
    {
        EnsureApp();
        ThrowIfNotInitialized();

        return DistributedApplication.GetConnectionString(resourceName);
    }

    /// <summary>
    /// Gets the endpoint for the specified resource.
    /// </summary>
    /// <param name="resourceName">The resource name.</param>
    /// <param name="endpointName">The optional endpoint name. If none are specified, the single defined endpoint is returned.</param>
    /// <returns>A URI representation of the endpoint.</returns>
    /// <exception cref="ArgumentException">The resource was not found, no matching endpoint was found, or multiple endpoints were found.</exception>
    /// <exception cref="InvalidOperationException">The resource has no endpoints.</exception>
    public Uri GetEndpoint(string resourceName, string? endpointName = default)
    {
        EnsureApp();
        ThrowIfNotInitialized();

        return DistributedApplication.GetEndpoint(resourceName, endpointName);
    }

    /// <summary>
    /// Called when the application builder is being created.
    /// </summary>
    /// <param name="applicationOptions">The application options.</param>
    /// <param name="hostOptions">The host builder options.</param>
    protected virtual void OnBuilderCreating(DistributedApplicationOptions applicationOptions, HostApplicationBuilderSettings hostOptions)
    {
    }

    /// <summary>
    /// Called when the application builder is created.
    /// </summary>
    /// <param name="applicationBuilder">The application builder.</param>
    protected virtual void OnBuilderCreated(DistributedApplicationBuilder applicationBuilder)
    {
    }

    /// <summary>
    /// Called when the application is being built.
    /// </summary>
    /// <param name="applicationBuilder">The application builder.</param>
    protected virtual void OnBuilding(DistributedApplicationBuilder applicationBuilder)
    {
    }

    private void OnBuilderCreatingCore(DistributedApplicationOptions applicationOptions, HostApplicationBuilderSettings hostBuilderOptions)
    {
        hostBuilderOptions.EnvironmentName = Environments.Development;
        hostBuilderOptions.ApplicationName = typeof(TEntryPoint).Assembly.GetName().Name ?? string.Empty;
        applicationOptions.AssemblyName = typeof(TEntryPoint).Assembly.GetName().Name ?? string.Empty;
        applicationOptions.DisableDashboard = true;
        OnBuilderCreating(applicationOptions, hostBuilderOptions);
    }

    private void OnBuilderCreatedCore(DistributedApplicationBuilder applicationBuilder)
    {
        OnBuilderCreated(applicationBuilder);
    }

    private void OnBuildingCore(DistributedApplicationBuilder applicationBuilder)
    {
        // Patch DcpOptions configuration
        var services = applicationBuilder.Services;
        services.RemoveAll<IConfigureOptions<DcpOptions>>();
        services.AddSingleton<IConfigureOptions<DcpOptions>, ConfigureDcpOptions>();
        services.Configure<DcpOptions>(o =>
        {
            o.ResourceNameSuffix = $"{Random.Shared.Next():x}";
            o.DeleteResourcesOnShutdown = true;
            o.RandomizePorts = true;
        });

        services.AddHttpClient();
        services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());

        InterceptHostCreation(applicationBuilder);

        OnBuilding(applicationBuilder);
    }

    [MemberNotNull(nameof(_app))]
    private void EnsureApp()
    {
        if (_app is not null)
        {
            return;
        }

        EnsureDepsFile();

        if (_appTask is null)
        {
            lock (_lockObj)
            {
                if (_appTask is null)
                {
                    // This helper launches the target assembly's entry point and hooks into the lifecycle
                    // so we can intercept execution at key stages.
                    var factory = DistributedApplicationEntryPointInvoker.ResolveEntryPoint(
                        typeof(TEntryPoint).Assembly,
                        onConstructing: OnBuilderCreatingCore,
                        onConstructed: OnBuilderCreatedCore,
                        onBuilding: OnBuildingCore,
                        entryPointCompleted: OnEntryPointExit);

                    if (factory is null)
                    {
                        throw new InvalidOperationException(
                            $"Could not intercept application builder instance. Ensure that {typeof(TEntryPoint)} is a type in an executable assembly, that the entrypoint creates an {typeof(DistributedApplicationBuilder)}, and that the resulting {typeof(DistributedApplication)} is being started.");
                    }

                    _appTask = ResolveApp(factory);
                }
            }
        }

        _app = _appTask.GetAwaiter().GetResult();
    }

    private void OnEntryPointExit(Exception? exception)
    {
        if (exception is not null)
        {
            _exitTcs.TrySetException(exception);
        }
        else
        {
            _exitTcs.TrySetResult();
        }
    }

    private async Task<DistributedApplication> ResolveApp(Func<string[], CancellationToken, Task<DistributedApplication>> factory)
    {
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        using var cts = new CancellationTokenSource(GetConfiguredTimeout());
        var app = await factory([], cts.Token).ConfigureAwait(false);
        _hostApplicationLifetime = app.Services.GetService<IHostApplicationLifetime>()
            ?? throw new InvalidOperationException($"Application did not register an implementation of {typeof(IHostApplicationLifetime)}.");
        return app;

        static TimeSpan GetConfiguredTimeout()
        {
            const string TimeoutEnvironmentKey = "DOTNET_HOST_FACTORY_RESOLVER_DEFAULT_TIMEOUT_IN_SECONDS";
            if (Debugger.IsAttached)
            {
                return Timeout.InfiniteTimeSpan;
            }

            if (uint.TryParse(Environment.GetEnvironmentVariable(TimeoutEnvironmentKey), out var timeoutInSeconds))
            {
                return TimeSpan.FromSeconds((int)timeoutInSeconds);
            }

            return TimeSpan.FromMinutes(5);
        }
    }

    private void ThrowIfNotInitialized()
    {
        if (!_startedTcs.Task.IsCompleted)
        {
            throw new InvalidOperationException("The application has not been initialized.");
        }
    }

    private static void EnsureDepsFile()
    {
        if (typeof(TEntryPoint).Assembly.EntryPoint == null)
        {
            throw new InvalidOperationException($"Assembly of specified type {typeof(TEntryPoint).Name} does not have an entry point.");
        }

        var depsFileName = $"{typeof(TEntryPoint).Assembly.GetName().Name}.deps.json";
        var depsFile = new FileInfo(Path.Combine(AppContext.BaseDirectory, depsFileName));
        if (!depsFile.Exists)
        {
            throw new InvalidOperationException($"Missing deps file '{Path.GetFileName(depsFile.FullName)}'. Make sure the project has been built.");
        }
    }

    /// <inheritdoc/>
    public virtual void Dispose()
    {
        if (_app is null || _hostApplicationLifetime is null)
        {
            return;
        }

        _hostApplicationLifetime?.StopApplication();
        _app?.Dispose();
    }

    /// <inheritdoc/>
    public virtual async ValueTask DisposeAsync()
    {
        if (_app is null)
        {
            return;
        }

        if (_hostApplicationLifetime is { } hostLifetime)
        {
            hostLifetime.StopApplication();

            // Wait for shutdown to complete.
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var _ = hostLifetime.ApplicationStopped.Register(s => ((TaskCompletionSource)s!).SetResult(), tcs);
            await tcs.Task.ConfigureAwait(false);
        }

        await _exitTcs.Task.ConfigureAwait(false);

        if (_app is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
    }

    // Replaces the IHost registration with an InterceptedHost registration which delegates to the original registration.
    private void InterceptHostCreation(DistributedApplicationBuilder applicationBuilder)
    {
        // Find the original IHost registration and remove it.
        var hostDescriptor = applicationBuilder.Services.Single(s => s.ServiceType == typeof(IHost) && s.ServiceKey is null);
        applicationBuilder.Services.Remove(hostDescriptor);

        // Insert the registration, modified to be a keyed service keyed on this factory instance.
        var interceptedDescriptor = hostDescriptor switch
        {
            { ImplementationFactory: { } factory } => ServiceDescriptor.KeyedSingleton<IHost>(this, (sp, _) => (IHost)factory(sp)),
            { ImplementationInstance: { } instance } => ServiceDescriptor.KeyedSingleton<IHost>(this, (IHost)instance),
            { ImplementationType: { } type } => ServiceDescriptor.KeyedSingleton(typeof(IHost), this, type),
            _ => throw new InvalidOperationException($"Registered service descriptor for {typeof(IHost)} does not conform to any known pattern.")
        };
        applicationBuilder.Services.Add(interceptedDescriptor);

        // Add a non-keyed registration which resolved the keyed registration, enabling interception.
        applicationBuilder.Services.AddSingleton<IHost>(sp => new ObservedHost(sp.GetRequiredKeyedService<IHost>(this), this));
    }

    private sealed class ObservedHost(IHost innerHost, DistributedApplicationTestingHarness<TEntryPoint> appFactory) : IHost, IAsyncDisposable
    {
        private bool _disposing;

        public IServiceProvider Services => innerHost.Services;

        public void Dispose()
        {
            if (_disposing)
            {
                return;
            }

            _disposing = true;
            innerHost.Dispose();
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (_disposing)
            {
                return;
            }

            _disposing = true;
            if (innerHost is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                innerHost.Dispose();
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await innerHost.StartAsync(cancellationToken).ConfigureAwait(false);
                appFactory._startedTcs.TrySetResult();
            }
            catch (Exception exception)
            {
                appFactory._startedTcs.TrySetException(exception);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken = default) => innerHost.StopAsync(cancellationToken);
    }

    private sealed class ConfigureDcpOptions(IConfiguration configuration) : IConfigureOptions<DcpOptions>
    {
        private const string DcpCliPathMetadataKey = "DcpCliPath";
        private const string DcpExtensionsPathMetadataKey = "DcpExtensionsPath";
        private const string DcpBinPathMetadataKey = "DcpBinPath";

        public void Configure(DcpOptions options)
        {
            var dcpPublisherConfiguration = configuration.GetSection("DcpPublisher");
            var publishingConfiguration = configuration.GetSection("Publishing");

            string? publisher = publishingConfiguration[nameof(PublishingOptions.Publisher)];
            string? cliPath;

            if (publisher is not null && publisher != "dcp")
            {
                // If DCP is not set as the publisher, don't calculate the DCP config
                return;
            }

            if (!string.IsNullOrEmpty(dcpPublisherConfiguration["CliPath"]))
            {
                // If an explicit path to DCP was provided from configuration, don't try to resolve via assembly attributes
                cliPath = dcpPublisherConfiguration["CliPath"];
                options.CliPath = cliPath;
            }
            else
            {
                var entryPointAssembly = typeof(TEntryPoint).Assembly;
                var assemblyMetadata = entryPointAssembly?.GetCustomAttributes<AssemblyMetadataAttribute>();
                cliPath = GetMetadataValue(assemblyMetadata, DcpCliPathMetadataKey);
                options.CliPath = cliPath;
                options.ExtensionsPath = GetMetadataValue(assemblyMetadata, DcpExtensionsPathMetadataKey);
                options.BinPath = GetMetadataValue(assemblyMetadata, DcpBinPathMetadataKey);
            }

            if (string.IsNullOrEmpty(cliPath))
            {
                throw new InvalidOperationException($"Could not resolve the path to the Aspire application host. The application cannot be run without it.");
            }
        }

        private static string? GetMetadataValue(IEnumerable<AssemblyMetadataAttribute>? assemblyMetadata, string key)
        {
            return assemblyMetadata?.FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase))?.Value;
        }
    }
}
