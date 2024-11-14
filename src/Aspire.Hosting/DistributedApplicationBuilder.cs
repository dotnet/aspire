// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Codespaces;
using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Health;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting;

/// <summary>
/// A builder for creating instances of <see cref="DistributedApplication"/>.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="DistributedApplicationBuilder"/> is the primary implementation of
/// <see cref="IDistributedApplicationBuilder"/> within .NET Aspire. Typically a developer
/// would interact with instances of this class via the <see cref="IDistributedApplicationBuilder"/>
/// interface which was created using one of the <see cref="DistributedApplication.CreateBuilder(string[])"/>
/// overloads.
/// </para>
/// <para>
/// For more information on how to configure the <see cref="DistributedApplication" /> using the
/// the builder pattern see <see cref="IDistributedApplicationBuilder" />.
/// </para>
/// </remarks>
public class DistributedApplicationBuilder : IDistributedApplicationBuilder
{
    private const string HostingDiagnosticListenerName = "Aspire.Hosting";
    private const string ApplicationBuildingEventName = "DistributedApplicationBuilding";
    private const string ApplicationBuiltEventName = "DistributedApplicationBuilt";
    private const string BuilderConstructingEventName = "DistributedApplicationBuilderConstructing";
    private const string BuilderConstructedEventName = "DistributedApplicationBuilderConstructed";

    private readonly DistributedApplicationOptions _options;

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
    public string AppHostPath { get; }

    /// <inheritdoc />
    public Assembly? AppHostAssembly => _options.Assembly;

    /// <inheritdoc />
    public DistributedApplicationExecutionContext ExecutionContext { get; }

    /// <inheritdoc />
    public IResourceCollection Resources { get; } = new ResourceCollection();

    /// <inheritdoc />
    public IDistributedApplicationEventing Eventing { get; } = new DistributedApplicationEventing();

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedApplicationBuilder"/> class with the specified options.
    /// </summary>
    /// <param name="args">The arguments provided to the builder.</param>
    /// <remarks>
    /// <para>
    /// Developers will not typically construct an instance of the <see cref="DistributedApplicationBuilder"/>
    /// class themselves and will instead use the <see cref="DistributedApplication.CreateBuilder(string[])"/>.
    /// This constructor is public to allow for some testing around extensibility scenarios.
    /// </para>
    /// </remarks>
    public DistributedApplicationBuilder(string[] args) : this(new DistributedApplicationOptions { Args = args })
    {
        ArgumentNullException.ThrowIfNull(args);
    }

    // This is here because in the constructor of DistributedApplicationBuilder we inject
    // DistributedApplicationExecutionContext. This is a class that is used to expose contextual
    // values in various callbacks and is a central location to access useful services like IServiceProvider.
    private readonly DistributedApplicationExecutionContextOptions _executionContextOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedApplicationBuilder"/> class with the specified options.
    /// </summary>
    /// <param name="options">The options for the distributed application.</param>
    /// <remarks>
    /// <para>
    /// Developers will not typically construct an instance of the <see cref="DistributedApplicationBuilder"/>
    /// class themselves and will instead use the <see cref="DistributedApplication.CreateBuilder(string[])"/>.
    /// This constructor is public to allow for some testing around extensibility scenarios.
    /// </para>
    /// <para>
    /// This constructor generates an instance of the <see cref="IDistributedApplicationBuilder"/> interface
    /// which is very similar to the instance that is returned from <see cref="DistributedApplication.CreateBuilder(string[])"/>
    /// however it is not guaranteed to be 100% consistent. For typical usage it is recommended that the
    /// <see cref="DistributedApplication.CreateBuilder(string[])"/> method is to create instances of
    /// the <see cref="IDistributedApplicationBuilder"/> interface.
    /// </para>
    /// </remarks>
    public DistributedApplicationBuilder(DistributedApplicationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;

        var innerBuilderOptions = new HostApplicationBuilderSettings();

        // Args are set later in config with switch mappings. But specify them when creating the builder
        // so they're used to initialize some types created immediately, e.g. IHostEnvironment.
        innerBuilderOptions.Args = options.Args;

        LogBuilderConstructing(options, innerBuilderOptions);
        _innerBuilder = new HostApplicationBuilder(innerBuilderOptions);

        _innerBuilder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
        _innerBuilder.Logging.AddFilter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Error);
        _innerBuilder.Logging.AddFilter("Aspire.Hosting.Dashboard", LogLevel.Error);
        _innerBuilder.Logging.AddFilter("Grpc.AspNetCore.Server.ServerCallHandler", LogLevel.Error);

        // This is to reduce log noise when we activate health checks for resources which may not yet be
        // fully initialized. For example a database which is not yet created.
        _innerBuilder.Logging.AddFilter("Microsoft.Extensions.Diagnostics.HealthChecks.DefaultHealthCheckService", LogLevel.None);

        // This is so that we can see certificate errors in the resource server in the console logs.
        // See: https://github.com/dotnet/aspire/issues/2914
        _innerBuilder.Logging.AddFilter("Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServer", LogLevel.Warning);

        // Add the logging configuration again to allow the user to override the defaults
        _innerBuilder.Logging.AddConfiguration(_innerBuilder.Configuration.GetSection("Logging"));

        AppHostDirectory = options.ProjectDirectory ?? _innerBuilder.Environment.ContentRootPath;
        var appHostName = options.ProjectName ?? _innerBuilder.Environment.ApplicationName;
        AppHostPath = Path.Join(AppHostDirectory, appHostName);

        // Set configuration
        ConfigurePublishingOptions(options);
        _innerBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            // Make the app host directory available to the application via configuration
            ["AppHost:Directory"] = AppHostDirectory,
            ["AppHost:Path"] = AppHostPath,
        });

        _executionContextOptions = _innerBuilder.Configuration["Publishing:Publisher"] switch
        {
            "manifest" => new DistributedApplicationExecutionContextOptions(DistributedApplicationOperation.Publish),
            _ => new DistributedApplicationExecutionContextOptions(DistributedApplicationOperation.Run)
        };

        ExecutionContext = new DistributedApplicationExecutionContext(_executionContextOptions);

        Eventing.Subscribe<BeforeResourceStartedEvent>(async (@event, ct) =>
        {
            var rns = @event.Services.GetRequiredService<ResourceNotificationService>();
            await rns.WaitForDependenciesAsync(@event.Resource, ct).ConfigureAwait(false);
        });

        // Conditionally configure AppHostSha based on execution context. For local scenarios, we want to
        // account for the path the AppHost is running from to disambiguate between different projects
        // with the same name as seen in https://github.com/dotnet/aspire/issues/5413. For publish scenarios,
        // we want to use a stable hash based only on the project name.
        string appHostSha;
        if (ExecutionContext.IsPublishMode)
        {
            var appHostNameShaBytes = SHA256.HashData(Encoding.UTF8.GetBytes(appHostName));
            appHostSha = Convert.ToHexString(appHostNameShaBytes);
        }
        else
        {
            var appHostShaBytes = SHA256.HashData(Encoding.UTF8.GetBytes(AppHostPath));
            appHostSha = Convert.ToHexString(appHostShaBytes);
        }
        _innerBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AppHost:Sha256"] = appHostSha
        });

        // Core things
        _innerBuilder.Services.AddSingleton(sp => new DistributedApplicationModel(Resources));
        _innerBuilder.Services.AddHostedService<DistributedApplicationLifecycle>();
        _innerBuilder.Services.AddHostedService<DistributedApplicationRunner>();
        _innerBuilder.Services.AddSingleton(options);
        _innerBuilder.Services.AddSingleton<ResourceNotificationService>();
        _innerBuilder.Services.AddSingleton<ResourceLoggerService>();
        _innerBuilder.Services.AddSingleton<IDistributedApplicationEventing>(Eventing);
        _innerBuilder.Services.AddHealthChecks();

        ConfigureHealthChecks();

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
                else
                {
                    // The dashboard is enabled but is unsecured. Set auth mode config setting to reflect this state.
                    _innerBuilder.Configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["AppHost:ResourceService:AuthMode"] = nameof(ResourceServiceAuthMode.Unsecured)
                        }
                    );
                }

                _innerBuilder.Services.AddSingleton<DashboardCommandExecutor>();
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
            _innerBuilder.Services.AddSingleton<DcpNameGenerator>();

            // We need a unique path per application instance
            _innerBuilder.Services.AddSingleton(new Locations());
            _innerBuilder.Services.AddSingleton<IKubernetesService, KubernetesService>();

            // Codespaces
            _innerBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<CodespacesOptions>, ConfigureCodespacesOptions>());
            _innerBuilder.Services.AddSingleton<CodespacesUrlRewriter>();
            _innerBuilder.Services.AddHostedService<CodespacesResourceUrlRewriterService>();

            Eventing.Subscribe<BeforeStartEvent>(BuiltInDistributedApplicationEventSubscriptionHandlers.InitializeDcpAnnotations);
        }

        // Publishing support
        Eventing.Subscribe<BeforeStartEvent>(BuiltInDistributedApplicationEventSubscriptionHandlers.MutateHttp2TransportAsync);
        _innerBuilder.Services.AddKeyedSingleton<IDistributedApplicationPublisher, ManifestPublisher>("manifest");

        Eventing.Subscribe<BeforeStartEvent>(BuiltInDistributedApplicationEventSubscriptionHandlers.ExcludeDashboardFromManifestAsync);

        // Overwrite registry if override specified in options
        if (!string.IsNullOrEmpty(options.ContainerRegistryOverride))
        {
            Eventing.Subscribe<BeforeStartEvent>((e, ct) => BuiltInDistributedApplicationEventSubscriptionHandlers.UpdateContainerRegistryAsync(e, options));
        }

        _innerBuilder.Services.AddSingleton(ExecutionContext);
        LogBuilderConstructed(this);
    }

    private void ConfigureHealthChecks()
    {
        _innerBuilder.Services.AddSingleton<IValidateOptions<HealthCheckServiceOptions>>(sp =>
        {
            var appModel = sp.GetRequiredService<DistributedApplicationModel>();
            var logger = sp.GetRequiredService<ILogger<DistributedApplicationBuilder>>();

            // Generic message (we update it in the callback to make it more specific).
            var failureMessage = "A health check registration is missing. Check logs for more details.";

            return new ValidateOptions<HealthCheckServiceOptions>(null, (options) =>
            {
                var resourceHealthChecks = appModel.Resources.SelectMany(
                    r => r.Annotations.OfType<HealthCheckAnnotation>().Select(hca => new { Resource = r, Annotation = hca })
                    );

                var healthCheckRegistrationKeys = options.Registrations.Select(hcr => hcr.Name).ToHashSet();
                var missingResourceHealthChecks = resourceHealthChecks.Where(rhc => !healthCheckRegistrationKeys.Contains(rhc.Annotation.Key));

                foreach (var missingResourceHealthCheck in missingResourceHealthChecks)
                {
                    sp.GetRequiredService<ILogger<DistributedApplicationBuilder>>().LogCritical(
                        "The health check '{Key}' is not registered and is required for resource '{ResourceName}'.",
                        missingResourceHealthCheck.Annotation.Key,
                        missingResourceHealthCheck.Resource.Name);
                }

                return !missingResourceHealthChecks.Any();
            }, failureMessage);
        });

        _innerBuilder.Services.AddSingleton<IConfigureOptions<HealthCheckPublisherOptions>>(sp =>
        {
            return new ConfigureOptions<HealthCheckPublisherOptions>(options =>
            {
                if (ExecutionContext.IsPublishMode)
                {
                    // In publish mode we don't run any checks.
                    options.Predicate = (check) => false;
                }
            });
        });

        if (ExecutionContext.IsRunMode)
        {
            _innerBuilder.Services.AddSingleton<ResourceHealthCheckService>();
            _innerBuilder.Services.AddHostedService<ResourceHealthCheckService>(sp => sp.GetRequiredService<ResourceHealthCheckService>());
        }
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
            { "--dcp-container-runtime", "DcpPublisher:ContainerRuntime" },
            { "--dcp-dependency-check-timeout", "DcpPublisher:DependencyCheckTimeout" },
            { "--dcp-dashboard-path", "DcpPublisher:DashboardPath" },
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

            _executionContextOptions.ServiceProvider = application.Services.GetRequiredService<IServiceProvider>();

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
            diagnosticListener.Write(BuilderConstructingEventName, (appBuilderOptions, hostBuilderOptions));
        }

        return diagnosticListener;
    }

    private static DiagnosticListener LogBuilderConstructed(DistributedApplicationBuilder builder)
    {
        var diagnosticListener = new DiagnosticListener(HostingDiagnosticListenerName);

        if (diagnosticListener.IsEnabled() && diagnosticListener.IsEnabled(BuilderConstructedEventName))
        {
            diagnosticListener.Write(BuilderConstructedEventName, builder);
        }

        return diagnosticListener;
    }

    private static DiagnosticListener LogAppBuilding(DistributedApplicationBuilder appBuilder)
    {
        var diagnosticListener = new DiagnosticListener(HostingDiagnosticListenerName);

        if (diagnosticListener.IsEnabled() && diagnosticListener.IsEnabled(ApplicationBuildingEventName))
        {
            diagnosticListener.Write(ApplicationBuildingEventName, appBuilder);
        }

        return diagnosticListener;
    }

    private static DiagnosticListener LogAppBuilt(DistributedApplication app)
    {
        var diagnosticListener = new DiagnosticListener(HostingDiagnosticListenerName);

        if (diagnosticListener.IsEnabled() && diagnosticListener.IsEnabled(ApplicationBuiltEventName))
        {
            diagnosticListener.Write(ApplicationBuiltEventName, app);
        }

        return diagnosticListener;
    }
}
