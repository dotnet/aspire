// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Aspire.Components.Common.Tests;
using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Tests.Dcp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Aspire.Hosting.Utils;

/// <summary>
/// DistributedApplication.CreateBuilder() creates a builder that includes configuration to read from appsettings.json.
/// The builder has a FileSystemWatcher, which can't be cleaned up unless a DistributedApplication is built and disposed.
/// This class wraps the builder and provides a way to automatically dispose it to prevent test failures from excessive
/// FileSystemWatcher instances from many tests.
/// </summary>
public sealed class TestDistributedApplicationBuilder : IDistributedApplicationBuilder, IDisposable
{
    private readonly DistributedApplicationBuilder _innerBuilder;
    private bool _disposedValue;
    private DistributedApplication? _app;

    public static TestDistributedApplicationBuilder Create(DistributedApplicationOperation operation)
    {
        var args = operation switch
        {
            DistributedApplicationOperation.Run => (string[])[],
            DistributedApplicationOperation.Publish => ["Publishing:Publisher=manifest"],
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        return Create(args);
    }

    // Returns the unique prefix used for volumes from unnamed volumes this builder
    public string GetVolumePrefix() =>
        $"{VolumeNameGenerator.Sanitize(Environment.ApplicationName).ToLowerInvariant()}-{Configuration["AppHost:Sha256"]!.ToLowerInvariant()[..10]}";

    public static TestDistributedApplicationBuilder Create(params string[] args)
    {
        return new TestDistributedApplicationBuilder(options => options.Args = args);
    }

    public static TestDistributedApplicationBuilder Create(ITestOutputHelper testOutputHelper, params string[] args)
    {
        return new TestDistributedApplicationBuilder(options => options.Args = args, testOutputHelper);
    }

    public static TestDistributedApplicationBuilder Create(Action<DistributedApplicationOptions>? configureOptions, ITestOutputHelper? testOutputHelper = null)
    {
        return new TestDistributedApplicationBuilder(configureOptions, testOutputHelper);
    }

    public static TestDistributedApplicationBuilder CreateWithTestContainerRegistry(ITestOutputHelper testOutputHelper) =>
        Create(o => o.ContainerRegistryOverride = ComponentTestConstants.AspireTestContainerRegistry, testOutputHelper);

    private TestDistributedApplicationBuilder(Action<DistributedApplicationOptions>? configureOptions, ITestOutputHelper? testOutputHelper = null)
    {
        var appAssembly = typeof(TestDistributedApplicationBuilder).Assembly;
        var assemblyName = appAssembly.FullName;

        _innerBuilder = BuilderInterceptor.CreateBuilder(Configure);

        _innerBuilder.Services.Configure<DashboardOptions>(o =>
        {
            // Make sure we have a dashboard URL and OTLP endpoint URL (but don't overwrite them if they're already set)
            o.DashboardUrl ??= "http://localhost:8080";
            o.OtlpGrpcEndpointUrl ??= "http://localhost:4317";
        });

        _innerBuilder.Services.AddSingleton<ApplicationExecutorProxy>(sp => new ApplicationExecutorProxy(sp.GetRequiredService<ApplicationExecutor>()));

        _innerBuilder.Services.AddHttpClient();
        _innerBuilder.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());
        if (testOutputHelper is not null)
        {
            WithTestAndResourceLogging(testOutputHelper);
        }

        void Configure(DistributedApplicationOptions applicationOptions, HostApplicationBuilderSettings hostBuilderOptions)
        {
            hostBuilderOptions.EnvironmentName = Environments.Development;
            hostBuilderOptions.ApplicationName = appAssembly.GetName().Name;
            applicationOptions.AssemblyName = assemblyName;
            applicationOptions.DisableDashboard = true;
            var cfg = hostBuilderOptions.Configuration ??= new();
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DcpPublisher:RandomizePorts"] = "true",
                ["DcpPublisher:DeleteResourcesOnShutdown"] = "true",
                ["DcpPublisher:ResourceNameSuffix"] = $"{Random.Shared.Next():x}",
            });

            configureOptions?.Invoke(applicationOptions);
        }
    }

    public TestDistributedApplicationBuilder WithTestAndResourceLogging(ITestOutputHelper testOutputHelper)
    {
        Services.AddXunitLogging(testOutputHelper);
        Services.AddHostedService<ResourceLoggerForwarderService>();
        Services.AddLogging(builder =>
        {
            builder.AddFilter("Aspire.Hosting", LogLevel.Trace);
            builder.SetMinimumLevel(LogLevel.Trace);
        });
        return this;
    }

    public ConfigurationManager Configuration => _innerBuilder.Configuration;

    public string AppHostDirectory => _innerBuilder.AppHostDirectory;

    public Assembly? AppHostAssembly => _innerBuilder.AppHostAssembly;

    public IHostEnvironment Environment => _innerBuilder.Environment;

    public IServiceCollection Services => _innerBuilder.Services;

    public DistributedApplicationExecutionContext ExecutionContext => _innerBuilder.ExecutionContext;

    public IResourceCollection Resources => _innerBuilder.Resources;

    public IDistributedApplicationEventing Eventing => _innerBuilder.Eventing;

    public IResourceBuilder<T> AddResource<T>(T resource) where T : IResource => _innerBuilder.AddResource(resource);

    [MemberNotNull(nameof(_app))]
    public DistributedApplication Build() => _app = _innerBuilder.Build();

    public Task<DistributedApplication> BuildAsync(CancellationToken cancellationToken = default) => Task.FromResult(Build());

    public IResourceBuilder<T> CreateResourceBuilder<T>(T resource) where T : IResource
    {
        return _innerBuilder.CreateResourceBuilder(resource);
    }

    public void Dispose()
    {
        if (!_disposedValue)
        {
            _disposedValue = true;
            if (_app is null)
            {
                try
                {
                    Build();
                }
                catch
                {
                }
            }

            _app?.Dispose();
        }
    }

    private sealed class BuilderInterceptor : IObserver<DiagnosticListener>
    {
        private static readonly ThreadLocal<BuilderInterceptor?> s_currentListener = new();
        private readonly ApplicationBuilderDiagnosticListener _applicationBuilderListener;
        private readonly Action<DistributedApplicationOptions, HostApplicationBuilderSettings>? _onConstructing;

        private BuilderInterceptor(Action<DistributedApplicationOptions, HostApplicationBuilderSettings>? onConstructing)
        {
            _onConstructing = onConstructing;
            _applicationBuilderListener = new(this);
        }

        public static DistributedApplicationBuilder CreateBuilder(Action<DistributedApplicationOptions, HostApplicationBuilderSettings> onConstructing)
        {
            var interceptor = new BuilderInterceptor(onConstructing);
            var original = s_currentListener.Value;
            s_currentListener.Value = interceptor;
            try
            {
                using var subscription = DiagnosticListener.AllListeners.Subscribe(interceptor);
                return new DistributedApplicationBuilder([]);
            }
            finally
            {
                s_currentListener.Value = original;
            }
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {

        }

        public void OnNext(DiagnosticListener value)
        {
            if (s_currentListener.Value != this)
            {
                // Ignore events that aren't for this listener
                return;
            }

            if (value.Name == "Aspire.Hosting")
            {
                _applicationBuilderListener.Subscribe(value);
            }
        }

        private sealed class ApplicationBuilderDiagnosticListener(BuilderInterceptor owner) : IObserver<KeyValuePair<string, object?>>
        {
            private IDisposable? _disposable;

            public void Subscribe(DiagnosticListener listener)
            {
                _disposable = listener.Subscribe(this);
            }

            public void OnCompleted()
            {
                _disposable?.Dispose();
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(KeyValuePair<string, object?> value)
            {
                if (s_currentListener.Value != owner)
                {
                    // Ignore events that aren't for this listener
                    return;
                }

                if (value.Key == "DistributedApplicationBuilderConstructing")
                {
                    var (options, innerBuilderOptions) = ((DistributedApplicationOptions, HostApplicationBuilderSettings))value.Value!;
                    owner._onConstructing?.Invoke(options, innerBuilderOptions);
                }
            }
        }
    }
}
