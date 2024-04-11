// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting;

/// <summary>
/// A builder for creating instances of <see cref="DistributedApplication"/>.
/// </summary>
public class DistributedApplicationBuilder : IDistributedApplicationBuilder
{
    private const string HostingDiagnosticListenerName = "Aspire.Hosting";
    private const string ApplicationBuildingEventName = "DistributedApplicationBuilding";
    private const string ApplicationBuiltEventName = "DistributedApplicationBuilt";
    private const string BuilderConstructingEventName = "DistributedApplicationBuilderConstructing";
    private const string BuilderConstructedEventName = "DistributedApplicationBuilderConstructed";

    private readonly HostApplicationBuilder _innerBuilder;

    /// <inheritdoc />
    public IHostEnvironment Environment => _innerBuilder.Environment;

    /// <inheritdoc />
    public ConfigurationManager Configuration => _innerBuilder.Configuration;

    /// <inheritdoc />
    public IServiceCollection Services => _innerBuilder.Services;

    /// <inheritdoc />
    public string AppHostDirectory { get; }

    /// <inheritdoc />
    public DistributedApplicationExecutionContext ExecutionContext { get; }

    /// <inheritdoc />
    public IResourceCollection Resources { get; } = new ResourceCollection();

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedApplicationBuilder"/> class with the specified options.
    /// </summary>
    /// <param name="args">The arguments provided to the builder.</param>
    public DistributedApplicationBuilder(string[] args) : this(new DistributedApplicationOptions { Args = args })
    {
        ArgumentNullException.ThrowIfNull(args);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedApplicationBuilder"/> class with the specified options.
    /// </summary>
    /// <param name="options">The options for the distributed application.</param>
    public DistributedApplicationBuilder(DistributedApplicationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var innerBuilderOptions = new HostApplicationBuilderSettings();

        // Args are set later in config with switch mappings. But specify them when creating the builder
        // so they're used to initialize some types created immediately, e.g. IHostEnvironment.
        innerBuilderOptions.Args = options.Args;

        LogBuilderConstructing(options, innerBuilderOptions);
        _innerBuilder = new HostApplicationBuilder(innerBuilderOptions);

        _innerBuilder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
        _innerBuilder.Logging.AddFilter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Error);

        // This is so that we can see certificate errors in the resource server in the console logs.
        // See: https://github.com/dotnet/aspire/issues/2914
        _innerBuilder.Logging.AddFilter("Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServer", LogLevel.Warning);

        AppHostDirectory = options.ProjectDirectory ?? _innerBuilder.Environment.ContentRootPath;

        // Set configuration
        ConfigurePublishingOptions(options);
        _innerBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            // Make the app host directory available to the application via configuration
            ["AppHost:Directory"] = AppHostDirectory
        });

        ExecutionContext = _innerBuilder.Configuration["Publishing:Publisher"] switch
        {
            "manifest" => new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish),
            _ => new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run)
        };

        // Core things
        _innerBuilder.Services.AddSingleton(sp => new DistributedApplicationModel(Resources));
        _innerBuilder.Services.AddHostedService<DistributedApplicationLifecycle>();
        _innerBuilder.Services.AddHostedService<DistributedApplicationRunner>();
        _innerBuilder.Services.AddSingleton(options);
        _innerBuilder.Services.AddSingleton<ResourceNotificationService>();
        _innerBuilder.Services.AddSingleton<ResourceLoggerService>();

        if (ExecutionContext.IsRunMode)
        {
            // Dashboard
            if (!options.DisableDashboard)
            {
                if (!IsDashboardUnsecured(_innerBuilder.Configuration))
                {
                    // Set a random API key for the OTLP exporter.
                    // Passed to apps as a standard OTEL attribute to include in OTLP requests and the dashboard to validate.
                    _innerBuilder.Configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["AppHost:OtlpApiKey"] = TokenGenerator.GenerateToken()
                        }
                    );

                    // Determine the frontend browser token.
                    if (_innerBuilder.Configuration[KnownConfigNames.DashboardFrontendBrowserToken] is not { Length: > 0 } browserToken)
                    {
                        // No browser token was specified in configuration, so generate one.
                        browserToken = TokenGenerator.GenerateToken();
                    }

                    _innerBuilder.Configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["AppHost:BrowserToken"] = browserToken
                        }
                    );

                    // Determine the resource service API key.
                    if (_innerBuilder.Configuration[KnownConfigNames.DashboardResourceServiceClientApiKey] is not { Length: > 0 } apiKey)
                    {
                        // No API key was specified in configuration, so generate one.
                        apiKey = TokenGenerator.GenerateToken();
                    }

                    _innerBuilder.Configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["AppHost:ResourceService:AuthMode"] = nameof(ResourceServiceAuthMode.ApiKey),
                            ["AppHost:ResourceService:ApiKey"] = apiKey
                        }
                    );
                }

                _innerBuilder.Services.AddOptions<TransportOptions>().ValidateOnStart().PostConfigure(MapTransportOptionsFromCustomKeys);
                _innerBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<TransportOptions>, TransportOptionsValidator>());
                _innerBuilder.Services.AddSingleton<DashboardServiceHost>();
                _innerBuilder.Services.AddHostedService(sp => sp.GetRequiredService<DashboardServiceHost>());
                _innerBuilder.Services.AddSingleton<IDashboardEndpointProvider, HostDashboardEndpointProvider>();
                _innerBuilder.Services.AddLifecycleHook<DashboardLifecycleHook>();
                _innerBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<DashboardOptions>, ConfigureDefaultDashboardOptions>());
                _innerBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<DashboardOptions>, ValidateDashboardOptions>());
            }

            // DCP stuff
            _innerBuilder.Services.AddSingleton<ApplicationExecutor>();
            _innerBuilder.Services.AddSingleton<IDcpDependencyCheckService, DcpDependencyCheck>();
            _innerBuilder.Services.AddHostedService<DcpHostService>();
            _innerBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<DcpOptions>, ConfigureDefaultDcpOptions>());
            _innerBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<DcpOptions>, ValidateDcpOptions>());

            // We need a unique path per application instance
            _innerBuilder.Services.AddSingleton(new Locations());
            _innerBuilder.Services.AddSingleton<IKubernetesService, KubernetesService>();
        }

        // Publishing support
        _innerBuilder.Services.AddLifecycleHook<Http2TransportMutationHook>();
        _innerBuilder.Services.AddKeyedSingleton<IDistributedApplicationPublisher, ManifestPublisher>("manifest");

        // Overwrite registry if override specified in options
        if (!string.IsNullOrEmpty(options.ContainerRegistryOverride))
        {
            _innerBuilder.Services.AddLifecycleHook<ContainerRegistryHook>();
        }

        _innerBuilder.Services.AddSingleton(ExecutionContext);
        LogBuilderConstructed(this);
    }

    private void MapTransportOptionsFromCustomKeys(TransportOptions options)
    {
        if (Configuration.GetBool(KnownConfigNames.AllowUnsecuredTransport) is { } allowUnsecuredTransport)
        {
            options.AllowUnsecureTransport = allowUnsecuredTransport;
        }
    }

    private static bool IsDashboardUnsecured(IConfiguration configuration)
    {
        return configuration.GetBool(KnownConfigNames.DashboardUnsecuredAllowAnonymous) ?? false;
    }

    private void ConfigurePublishingOptions(DistributedApplicationOptions options)
    {
        var switchMappings = new Dictionary<string, string>()
        {
            { "--publisher", "Publishing:Publisher" },
            { "--output-path", "Publishing:OutputPath" },
            { "--dcp-cli-path", "DcpPublisher:CliPath" },
            { "--container-runtime", "DcpPublisher:ContainerRuntime" },
            { "--dependency-check-timeout", "DcpPublisher:DependencyCheckTimeout" },
        };
        _innerBuilder.Configuration.AddCommandLine(options.Args ?? [], switchMappings);
        _innerBuilder.Services.Configure<PublishingOptions>(_innerBuilder.Configuration.GetSection(PublishingOptions.Publishing));
    }

    /// <inheritdoc />
    public DistributedApplication Build()
    {
        AspireEventSource.Instance.DistributedApplicationBuildStart();
        try
        {
            LogAppBuilding(this);

            // AddResource(resource) validates that a name is unique but it's possible to add resources directly to the resource collection.
            // Validate names for duplicates while building the application.
            foreach (var duplicateResourceName in Resources.GroupBy(r => r.Name, StringComparers.ResourceName)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key))
            {
                throw new DistributedApplicationException($"Multiple resources with the name '{duplicateResourceName}'. Resource names are case-insensitive.");
            }

            var application = new DistributedApplication(_innerBuilder.Build());
            LogAppBuilt(application);
            return application;
        }
        finally
        {
            AspireEventSource.Instance.DistributedApplicationBuildStop();
        }
    }

    /// <inheritdoc />
    public IResourceBuilder<T> AddResource<T>(T resource) where T : IResource
    {
        ArgumentNullException.ThrowIfNull(resource);

        if (Resources.FirstOrDefault(r => string.Equals(r.Name, resource.Name, StringComparisons.ResourceName)) is { } existingResource)
        {
            throw new DistributedApplicationException($"Cannot add resource of type '{resource.GetType()}' with name '{resource.Name}' because resource of type '{existingResource.GetType()}' with that name already exists. Resource names are case-insensitive.");
        }

        Resources.Add(resource);
        return CreateResourceBuilder(resource);
    }

    /// <inheritdoc />
    public IResourceBuilder<T> CreateResourceBuilder<T>(T resource) where T : IResource
    {
        ArgumentNullException.ThrowIfNull(resource);

        var builder = new DistributedApplicationResourceBuilder<T>(this, resource);
        return builder;
    }

    private static DiagnosticListener LogBuilderConstructing(DistributedApplicationOptions appBuilderOptions, HostApplicationBuilderSettings hostBuilderOptions)
    {
        var diagnosticListener = new DiagnosticListener(HostingDiagnosticListenerName);

        if (diagnosticListener.IsEnabled() && diagnosticListener.IsEnabled(BuilderConstructingEventName))
        {
            Write(diagnosticListener, BuilderConstructingEventName, (appBuilderOptions, hostBuilderOptions));
        }

        return diagnosticListener;
    }

    private static DiagnosticListener LogBuilderConstructed(DistributedApplicationBuilder builder)
    {
        var diagnosticListener = new DiagnosticListener(HostingDiagnosticListenerName);

        if (diagnosticListener.IsEnabled() && diagnosticListener.IsEnabled(BuilderConstructedEventName))
        {
            Write(diagnosticListener, BuilderConstructedEventName, builder);
        }

        return diagnosticListener;
    }

    private static DiagnosticListener LogAppBuilding(DistributedApplicationBuilder appBuilder)
    {
        var diagnosticListener = new DiagnosticListener(HostingDiagnosticListenerName);

        if (diagnosticListener.IsEnabled() && diagnosticListener.IsEnabled(ApplicationBuildingEventName))
        {
            Write(diagnosticListener, ApplicationBuildingEventName, appBuilder);
        }

        return diagnosticListener;
    }

    private static DiagnosticListener LogAppBuilt(DistributedApplication app)
    {
        var diagnosticListener = new DiagnosticListener(HostingDiagnosticListenerName);

        if (diagnosticListener.IsEnabled() && diagnosticListener.IsEnabled(ApplicationBuiltEventName))
        {
            Write(diagnosticListener, ApplicationBuiltEventName, app);
        }

        return diagnosticListener;
    }

    // Remove when https://github.com/dotnet/runtime/pull/78532 is merged and consumed by the used SDK.
#if NET7_0
        [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
            Justification = "DiagnosticSource is used here to pass objects in-memory to code using HostFactoryResolver. This won't require creating new generic types.")]
#endif
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
        Justification = "The values being passed into Write are being consumed by the application already.")]
    private static void Write<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(
        DiagnosticListener diagnosticSource,
        string name,
        T value)
    {
        diagnosticSource.Write(name, value);
    }
}
