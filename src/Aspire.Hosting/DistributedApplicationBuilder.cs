// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Backchannel;
using Aspire.Hosting.Cli;
using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Devcontainers;
using Aspire.Hosting.Devcontainers.Codespaces;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Health;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Orchestrator;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Tools;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.SecretManager.Tools.Internal;

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

    private DistributedApplicationExecutionContextOptions BuildExecutionContextOptions()
    {
        var operationConfiguration = _innerBuilder.Configuration["AppHost:Operation"];
        if (operationConfiguration is null)
        {
            return _innerBuilder.Configuration["Publishing:Publisher"] switch
            {
                { } publisher => new DistributedApplicationExecutionContextOptions(DistributedApplicationOperation.Publish, publisher),
                _ => new DistributedApplicationExecutionContextOptions(DistributedApplicationOperation.Run)
            };
        }

        var operation = _innerBuilder.Configuration["AppHost:Operation"]?.ToLowerInvariant() switch
        {
            "publish" => DistributedApplicationOperation.Publish,
            "run" => DistributedApplicationOperation.Run,
            "tool" => DistributedApplicationOperation.Tool,
            _ => throw new DistributedApplicationException("Invalid operation specified. Valid operations are 'publish' or 'run'.")
        };

        return operation switch
        {
            DistributedApplicationOperation.Run or DistributedApplicationOperation.Tool => new DistributedApplicationExecutionContextOptions(operation),
            DistributedApplicationOperation.Publish => new DistributedApplicationExecutionContextOptions(operation, _innerBuilder.Configuration["Publishing:Publisher"] ?? "manifest"),
            _ => throw new DistributedApplicationException("Invalid operation specified. Valid operations are 'publish' or 'run'.")
        };
    }

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

        _innerBuilder.Services.AddSingleton(TimeProvider.System);

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

        var assemblyMetadata = AppHostAssembly?.GetCustomAttributes<AssemblyMetadataAttribute>();
        var aspireDir = GetMetadataValue(assemblyMetadata, "AppHostProjectBaseIntermediateOutputPath");

        // Set configuration
        ConfigurePublishingOptions(options);
        ConfigureToolOptions(options);
        _innerBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            // Make the app host directory available to the application via configuration
            ["AppHost:Directory"] = AppHostDirectory,
            ["AppHost:Path"] = AppHostPath,
            [AspireStore.AspireStorePathKeyName] = aspireDir
        });

        _executionContextOptions = BuildExecutionContextOptions();
        ExecutionContext = new DistributedApplicationExecutionContext(_executionContextOptions);

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
        _innerBuilder.Services.AddSingleton<ResourceCommandService>(s => new ResourceCommandService(s.GetRequiredService<ResourceNotificationService>(), s.GetRequiredService<ResourceLoggerService>(), s));
        _innerBuilder.Services.AddSingleton<IDistributedApplicationEventing>(Eventing);
        _innerBuilder.Services.AddHealthChecks();
        _innerBuilder.Services.Configure<ResourceNotificationServiceOptions>(o =>
        {
            // Default to stopping on dependency failure if the dashboard is disabled. As there's no way to see or easily recover
            // from a failure in that case.
            o.DefaultWaitBehavior = options.DisableDashboard ? WaitBehavior.StopOnResourceUnavailable : WaitBehavior.WaitOnResourceUnavailable;
        });
        _innerBuilder.Services.AddSingleton<IAspireStore, AspireStore>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var aspireDir = configuration[AspireStore.AspireStorePathKeyName];

            if (string.IsNullOrWhiteSpace(aspireDir))
            {
                throw new InvalidOperationException($"Could not determine an appropriate location for local storage. Set the {AspireStore.AspireStorePathKeyName} setting to a folder where the App Host content should be stored.");
            }

            return new AspireStore(Path.Combine(aspireDir, ".aspire"));
        });

        // Shared DCP things (even though DCP isn't used in 'publish' and 'inspect' mode
        // we still honour the DCP options around container runtime selection.
        _innerBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<DcpOptions>, ConfigureDefaultDcpOptions>());
        _innerBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<DcpOptions>, ValidateDcpOptions>());

        // Aspire CLI support
        _innerBuilder.Services.AddHostedService<CliOrphanDetector>();
        _innerBuilder.Services.AddSingleton<BackchannelService>();
        _innerBuilder.Services.AddHostedService<BackchannelService>(sp => sp.GetRequiredService<BackchannelService>());
        _innerBuilder.Services.AddSingleton<AppHostRpcTarget>();

        ConfigureHealthChecks();

        if (ExecutionContext.IsRunMode)
        {
            // Dashboard
            if (!options.DisableDashboard)
            {
                if (!IsDashboardUnsecured(_innerBuilder.Configuration))
                {
                    // Passed to apps as a standard OTEL attribute to include in OTLP requests and the dashboard to validate.
                    // Set a random API key for the OTLP exporter if one isn't already present in configuration.
                    // If a key is generated, it's stored in the user secrets store so that it will be auto-loaded
                    // on subsequent runs and not recreated. This is important to ensure it doesn't change the state
                    // of persistent containers (as a new key would be a spec change).
                    SecretsStore.GetOrSetUserSecret(_innerBuilder.Configuration, AppHostAssembly, "AppHost:OtlpApiKey", TokenGenerator.GenerateToken);

                    // Determine the frontend browser token.
                    if (_innerBuilder.Configuration.GetString(KnownConfigNames.DashboardFrontendBrowserToken,
                                                              KnownConfigNames.Legacy.DashboardFrontendBrowserToken, fallbackOnEmpty: true) is not { } browserToken)
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
                    var apiKey = _innerBuilder.Configuration.GetString(KnownConfigNames.DashboardResourceServiceClientApiKey,
                                                                       KnownConfigNames.Legacy.DashboardResourceServiceClientApiKey, fallbackOnEmpty: true);

                    // If no API key was specified in configuration, generate one.
                    apiKey ??= TokenGenerator.GenerateToken();

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

                _innerBuilder.Services.AddOptions<TransportOptions>().ValidateOnStart().PostConfigure(MapTransportOptionsFromCustomKeys);
                _innerBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<TransportOptions>, TransportOptionsValidator>());
                _innerBuilder.Services.AddSingleton<DashboardServiceHost>();
                _innerBuilder.Services.AddHostedService(sp => sp.GetRequiredService<DashboardServiceHost>());
                _innerBuilder.Services.AddSingleton<IDashboardEndpointProvider, HostDashboardEndpointProvider>();
                _innerBuilder.Services.AddLifecycleHook<DashboardLifecycleHook>();
                _innerBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<DashboardOptions>, ConfigureDefaultDashboardOptions>());
                _innerBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<DashboardOptions>, ValidateDashboardOptions>());

                ConfigureDashboardHealthCheck();
            }

            // Devcontainers & Codespaces
            _innerBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<CodespacesOptions>, ConfigureCodespacesOptions>());
            _innerBuilder.Services.AddSingleton<CodespacesUrlRewriter>();
            _innerBuilder.Services.AddHostedService<CodespacesResourceUrlRewriterService>();
            _innerBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<DevcontainersOptions>, ConfigureDevcontainersOptions>());
            _innerBuilder.Services.AddSingleton<DevcontainerSettingsWriter>();
            _innerBuilder.Services.TryAddLifecycleHook<DevcontainerPortForwardingLifecycleHook>();

            Eventing.Subscribe<BeforeStartEvent>(BuiltInDistributedApplicationEventSubscriptionHandlers.InitializeDcpAnnotations);
        }

        if (ExecutionContext.IsRunMode || ExecutionContext.IsToolMode)
        {
            if (options.EnableResourceLogging)
            {
                // This must be added before DcpHostService to ensure that it can subscribe to the ResourceNotificationService and ResourceLoggerService
                _innerBuilder.Services.AddHostedService<ResourceLoggerForwarderService>();
            }

            _innerBuilder.Services.AddSingleton<ToolExecutionService>();

            // Orchestrator
            _innerBuilder.Services.AddSingleton<ApplicationOrchestrator>();
            _innerBuilder.Services.AddHostedService<OrchestratorHostService>();

            // DCP stuff
            _innerBuilder.Services.AddSingleton<IDcpExecutor, DcpExecutor>();
            _innerBuilder.Services.AddSingleton<DcpExecutorEvents>();
            _innerBuilder.Services.AddSingleton<DcpHost>();
            _innerBuilder.Services.AddSingleton<IDcpDependencyCheckService, DcpDependencyCheck>();
            _innerBuilder.Services.AddSingleton<DcpNameGenerator>();

            // We need a unique path per application instance
            _innerBuilder.Services.AddSingleton(new Locations());
            _innerBuilder.Services.AddSingleton<IKubernetesService, KubernetesService>();
        }

        // Publishing support
        Eventing.Subscribe<BeforeStartEvent>(BuiltInDistributedApplicationEventSubscriptionHandlers.MutateHttp2TransportAsync);
        this.AddPublisher<ManifestPublisher, PublishingOptions>("manifest");
        this.AddPublisher<Publisher, PublishingOptions>("default");
        _innerBuilder.Services.AddKeyedSingleton<IContainerRuntime, DockerContainerRuntime>("docker");
        _innerBuilder.Services.AddKeyedSingleton<IContainerRuntime, PodmanContainerRuntime>("podman");
        _innerBuilder.Services.AddSingleton<IResourceContainerImageBuilder, ResourceContainerImageBuilder>();
        _innerBuilder.Services.AddSingleton<PublishingActivityProgressReporter>();
        _innerBuilder.Services.AddSingleton<IPublishingActivityProgressReporter, PublishingActivityProgressReporter>(sp => sp.GetRequiredService<PublishingActivityProgressReporter>());

        Eventing.Subscribe<BeforeStartEvent>(BuiltInDistributedApplicationEventSubscriptionHandlers.ExcludeDashboardFromManifestAsync);

        // Overwrite registry if override specified in options
        if (!string.IsNullOrEmpty(options.ContainerRegistryOverride))
        {
            Eventing.Subscribe<BeforeStartEvent>((e, ct) => BuiltInDistributedApplicationEventSubscriptionHandlers.UpdateContainerRegistryAsync(e, options));
        }

        _innerBuilder.Services.AddSingleton(ExecutionContext);
        LogBuilderConstructed(this);
    }

    private void ConfigureDashboardHealthCheck()
    {
        _innerBuilder.Services.AddHealthChecks().AddUrlGroup(sp => {

            var dashboardOptions = sp.GetRequiredService<IOptions<DashboardOptions>>().Value;
            if (StringUtils.TryGetUriFromDelimitedString(dashboardOptions.DashboardUrl, ";", out var firstDashboardUrl))
            {
                // Health checks to the dashboard should go to the /health endpoint. This endpoint allows anonymous requests.
                // Sending a request to other dashboard endpoints triggered auth, which the request fails, and is redirected to the login page.
                var uriBuilder = new UriBuilder(firstDashboardUrl);
                uriBuilder.Path = "/health";
                return uriBuilder.Uri;
            }
            else
            {
                throw new DistributedApplicationException($"The dashboard resource '{KnownResourceNames.AspireDashboard}' does not have endpoints.");
            }
        }, KnownHealthCheckNames.DashboardHealthCheck);

        _innerBuilder.Services.SuppressHealthCheckHttpClientLogging(KnownHealthCheckNames.DashboardHealthCheck);
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
        return configuration.GetBool(KnownConfigNames.DashboardUnsecuredAllowAnonymous, KnownConfigNames.Legacy.DashboardUnsecuredAllowAnonymous) ?? false;
    }

    private void ConfigurePublishingOptions(DistributedApplicationOptions options)
    {
        var switchMappings = new Dictionary<string, string>()
        {
            { "--operation", "AppHost:Operation" },
            { "--publisher", "Publishing:Publisher" },
            { "--output-path", "Publishing:OutputPath" },
            { "--deploy", "Publishing:Deploy" },
            { "--dcp-cli-path", "DcpPublisher:CliPath" },
            { "--dcp-container-runtime", "DcpPublisher:ContainerRuntime" },
            { "--dcp-dependency-check-timeout", "DcpPublisher:DependencyCheckTimeout" },
            { "--dcp-dashboard-path", "DcpPublisher:DashboardPath" }
        };
        _innerBuilder.Configuration.AddCommandLine(options.Args ?? [], switchMappings);
        _innerBuilder.Services.Configure<PublishingOptions>(_innerBuilder.Configuration.GetSection(PublishingOptions.Publishing));
    }

    private void ConfigureToolOptions(DistributedApplicationOptions options)
    {
        var switchMappings = new Dictionary<string, string>()
        {
            { "--operation", "AppHost:Operation" },
            { "--tool", "Tool:Resource" },
            { "--project", "Tool:Project" }
        };
        _innerBuilder.Configuration.AddCommandLine(options.Args ?? [], switchMappings);
        _innerBuilder.Services.Configure<ToolOptions>(_innerBuilder.Configuration.GetSection(ToolOptions.Section));

        var filteredArgs = new List<string>();
        var args = options.Args ?? Array.Empty<string>();
        for (int i = 0; i < args.Length;)
        {
            var arg = args[i];
            if (switchMappings.ContainsKey(arg))
            {
                i++;
                if (i < args.Length && !args[i].StartsWith("--"))
                {
                    i++;
                }
            }
            else
            {
                filteredArgs.Add(arg);
                i++;
            }
        }

        _innerBuilder.Services.PostConfigure<ToolOptions>(toolOptions =>
        {
            toolOptions.Args = filteredArgs.ToArray();
        });
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

    /// <summary>
    /// Gets the metadata value for the specified key from the assembly metadata.
    /// </summary>
    /// <param name="assemblyMetadata">The assembly metadata.</param>
    /// <param name="key">The key to look for.</param>
    /// <returns>The metadata value if found; otherwise, null.</returns>
    private static string? GetMetadataValue(IEnumerable<AssemblyMetadataAttribute>? assemblyMetadata, string key) =>
        assemblyMetadata?.FirstOrDefault(a => string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase))?.Value;
}
