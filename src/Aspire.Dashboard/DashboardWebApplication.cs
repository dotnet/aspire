// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using Aspire.Dashboard.Authentication;
using Aspire.Dashboard.Authentication.Connection;
using Aspire.Dashboard.Authentication.OpenIdConnect;
using Aspire.Dashboard.Authentication.OtlpApiKey;
using Aspire.Dashboard.Components;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp;
using Aspire.Dashboard.Otlp.Grpc;
using Aspire.Dashboard.Otlp.Http;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Telemetry;
using Aspire.Dashboard.Utils;
using Aspire.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Aspire.Dashboard;

public sealed class DashboardWebApplication : IAsyncDisposable
{
    private const string DashboardAuthCookieName = ".Aspire.Dashboard.Auth";
    private const string DashboardAntiForgeryCookieName = ".Aspire.Dashboard.Antiforgery";

    private readonly WebApplication _app;
    private readonly ILogger<DashboardWebApplication> _logger;
    private readonly IOptionsMonitor<DashboardOptions> _dashboardOptionsMonitor;
    private readonly IReadOnlyList<string> _validationFailures;
    private readonly List<Func<EndpointInfo>> _frontendEndPointAccessor = new();
    private Func<EndpointInfo>? _otlpServiceGrpcEndPointAccessor;
    private Func<EndpointInfo>? _otlpServiceHttpEndPointAccessor;

    public List<Func<EndpointInfo>> FrontendEndPointsAccessor
    {
        get
        {
            if (_frontendEndPointAccessor.Count == 0)
            {
                throw new InvalidOperationException("WebApplication not started yet.");
            }

            return _frontendEndPointAccessor;
        }
    }

    public Func<EndpointInfo> FrontendSingleEndPointAccessor
    {
        get
        {
            if (_frontendEndPointAccessor.Count == 0)
            {
                throw new InvalidOperationException("WebApplication not started yet.");
            }
            else if (_frontendEndPointAccessor.Count > 1)
            {
                throw new InvalidOperationException("Multiple frontend endpoints.");
            }

            return _frontendEndPointAccessor[0];
        }
    }

    public Func<EndpointInfo> OtlpServiceGrpcEndPointAccessor
    {
        get => _otlpServiceGrpcEndPointAccessor ?? throw new InvalidOperationException("WebApplication not started yet.");
    }

    public Func<EndpointInfo> OtlpServiceHttpEndPointAccessor
    {
        get => _otlpServiceHttpEndPointAccessor ?? throw new InvalidOperationException("WebApplication not started yet.");
    }

    public IOptionsMonitor<DashboardOptions> DashboardOptionsMonitor => _dashboardOptionsMonitor;

    public IReadOnlyList<string> ValidationFailures => _validationFailures;

    public IServiceProvider Services { get; }

    /// <summary>
    /// Create a new instance of the <see cref="DashboardWebApplication"/> class.
    /// </summary>
    /// <param name="preConfigureBuilder">Configuration for the internal app builder *before* normal dashboard configuration is done. This is for unit testing.</param>
    /// <param name="options">Environment configuration for the internal app builder. This is for unit testing</param>
    public DashboardWebApplication(
        Action<WebApplicationBuilder>? preConfigureBuilder = null,
        WebApplicationOptions? options = null)
    {
        var builder = options is not null ? WebApplication.CreateBuilder(options) : WebApplication.CreateBuilder();

        preConfigureBuilder?.Invoke(builder);

#if !DEBUG
        builder.Logging.AddFilter("Default", LogLevel.Information);
        builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
        // Suppress TokenDeserializeException error log from anti-forgery.
        // When dashboard is upgrade or run in a container the old anti-forgery cookie is no longer valid on first request.
        // Silently ignore and allow anti-forgery to automatically create a new valid cookie.
        builder.Logging.AddFilter("Microsoft.AspNetCore.Antiforgery.DefaultAntiforgery", LogLevel.None);
        builder.Logging.AddFilter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Error);
        builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.None);
#else

        // Log more when running the dashboard as debug.
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
        builder.Logging.AddFilter("Aspire.Dashboard", LogLevel.Debug);

        // Don't log routine dashboard HTTP request info or static file access
        // These logs generate a lot of noise when locally debugging.
        builder.Logging.AddFilter("Grpc", LogLevel.Information);
        builder.Logging.AddFilter("Aspire.Dashboard.Authentication", LogLevel.Information);
        builder.Logging.AddFilter("Aspire.Dashboard.Otlp", LogLevel.Information);
        builder.Logging.AddFilter("Microsoft", LogLevel.Information);
        builder.Logging.AddFilter("Microsoft.AspNetCore.Cors", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.AspNetCore.StaticFiles.StaticFileMiddleware", LogLevel.Warning);
#endif

        // Allow for a user specified JSON config file on disk. Throw an error if the specified file doesn't exist.
        if (builder.Configuration.GetString(DashboardConfigNames.DashboardConfigFilePathName.ConfigKey,
                                            DashboardConfigNames.Legacy.DashboardConfigFilePathName.ConfigKey, fallbackOnEmpty: true) is { } configFilePath)
        {
            builder.Configuration.AddJsonFile(configFilePath, optional: false, reloadOnChange: true);
        }

        // Allow for a user specified config directory on disk (e.g. for Docker secrets). Throw an error if the specified directory doesn't exist.
        if (builder.Configuration.GetString(DashboardConfigNames.DashboardFileConfigDirectoryName.ConfigKey,
                                            DashboardConfigNames.Legacy.DashboardFileConfigDirectoryName.ConfigKey, fallbackOnEmpty: true) is { } fileConfigDirectory)
        {
            builder.Configuration.AddKeyPerFile(directoryPath: fileConfigDirectory, optional: false, reloadOnChange: true);
        }

        var dashboardConfigSection = builder.Configuration.GetSection("Dashboard");
        builder.Services.AddOptions<DashboardOptions>()
            .Bind(dashboardConfigSection)
            .ValidateOnStart();
        builder.Services.AddSingleton<IPostConfigureOptions<DashboardOptions>, PostConfigureDashboardOptions>();
        builder.Services.AddSingleton<IValidateOptions<DashboardOptions>, ValidateDashboardOptions>();

        if (!TryGetDashboardOptions(builder, dashboardConfigSection, out var dashboardOptions, out var failureMessages))
        {
            // The options have validation failures. Write them out to the user and return a non-zero exit code.
            // We don't want to start the app, but we need to build the app to access the logger to log the errors.
            _app = builder.Build();
            _dashboardOptionsMonitor = _app.Services.GetRequiredService<IOptionsMonitor<DashboardOptions>>();
            _validationFailures = failureMessages.ToList();
            _logger = GetLogger();
            Services = _app.Services;
            WriteVersion(_logger);
            WriteValidationFailures(_logger, _validationFailures);
            return;
        }
        else
        {
            _validationFailures = Array.Empty<string>();
        }

        ConfigureKestrelEndpoints(builder, dashboardOptions);

        var browserHttpsPort = dashboardOptions.Frontend.GetEndpointAddresses().FirstOrDefault(IsHttpsOrNull)?.Port;
        var isAllHttps = browserHttpsPort is not null && IsHttpsOrNull(dashboardOptions.Otlp.GetGrpcEndpointAddress()) && IsHttpsOrNull(dashboardOptions.Otlp.GetHttpEndpointAddress());
        if (isAllHttps)
        {
            // Explicitly configure the HTTPS redirect port as we're possibly listening on multiple HTTPS addresses
            // if the dashboard OTLP URL is configured to use HTTPS too
            builder.Services.Configure<HttpsRedirectionOptions>(options => options.HttpsPort = browserHttpsPort);
        }

        builder.Services.AddSingleton<IPolicyEvaluator, AspirePolicyEvaluator>();

        ConfigureAuthentication(builder, dashboardOptions);

        // Add services to the container.
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            // Limit to compressing static text assets to mitigate user supplied data being compressed over HTTPS
            // See https://learn.microsoft.com/aspnet/core/performance/response-compression#compression-with-https for more information
            options.MimeTypes = ["text/javascript", "application/javascript", "text/css", "image/svg+xml"];
        });
        if (dashboardOptions.Otlp.Cors.IsCorsEnabled)
        {
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(OtlpHttpEndpointsBuilder.CorsPolicyName, builder =>
                {
                    var corsOptions = dashboardOptions.Otlp.Cors;

                    builder.WithOrigins(corsOptions.AllowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    builder.SetIsOriginAllowedToAllowWildcardSubdomains();

                    // By default, allow headers in the implicit safelist and X-Requested-With. This matches OTLP collector CORS behavior.
                    // Implicit safelist: https://developer.mozilla.org/en-US/docs/Glossary/CORS-safelisted_request_header
                    // OTLP collector: https://github.com/open-telemetry/opentelemetry-collector/blob/685625abb4703cb2e45a397f008127bbe2ba4c0e/config/confighttp/README.md#server-configuration
                    var allowedHeaders = !string.IsNullOrEmpty(corsOptions.AllowedHeaders)
                        ? corsOptions.AllowedHeaders.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        : ["X-Requested-With"];
                    builder.WithHeaders(allowedHeaders);

                    // Hardcode to allow only POST methods. OTLP is always sent in POST request bodies.
                    builder.WithMethods(HttpMethods.Post);
                });
            });
        }

        // Data from the server.
        builder.Services.TryAddSingleton<IDashboardClient, DashboardClient>();
        builder.Services.TryAddScoped<DashboardCommandExecutor>();

        builder.Services.AddSingleton<PauseManager>();

        // Telemetry
        builder.Services.TryAddSingleton<DashboardTelemetryService>();
        builder.Services.TryAddSingleton<IDashboardTelemetrySender, DashboardTelemetrySender>();

        // OTLP services.
        builder.Services.AddGrpc();
        builder.Services.AddSingleton<TelemetryRepository>();
        builder.Services.AddTransient<StructuredLogsViewModel>();

        builder.Services.AddTransient<OtlpLogsService>();
        builder.Services.AddTransient<OtlpTraceService>();
        builder.Services.AddTransient<OtlpMetricsService>();

        builder.Services.AddTransient<TracesViewModel>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IOutgoingPeerResolver, ResourceOutgoingPeerResolver>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IOutgoingPeerResolver, BrowserLinkOutgoingPeerResolver>());

        builder.Services.AddFluentUIComponents();

        builder.Services.AddScoped<IThemeResolver, BrowserThemeResolver>();
        builder.Services.AddScoped<ThemeManager>();
        // ShortcutManager is scoped because we want shortcuts to apply one browser window.
        builder.Services.AddScoped<ShortcutManager>();
        builder.Services.AddScoped<ConsoleLogsManager>();
        builder.Services.AddSingleton<IInstrumentUnitResolver, DefaultInstrumentUnitResolver>();

        // Time zone is set by the browser.
        builder.Services.AddScoped<BrowserTimeProvider>();
        builder.Services.AddScoped<ILocalStorage, LocalBrowserStorage>();
        builder.Services.AddScoped<ISessionStorage, SessionBrowserStorage>();

        builder.Services.AddSingleton<IKnownPropertyLookup, KnownPropertyLookup>();

        builder.Services.AddScoped<DimensionManager>();

        builder.Services.AddLocalization();

        builder.Services.AddAntiforgery(options =>
        {
            options.Cookie.Name = DashboardAntiForgeryCookieName;
        });

        _app = builder.Build();

        _dashboardOptionsMonitor = _app.Services.GetRequiredService<IOptionsMonitor<DashboardOptions>>();

        Services = _app.Services;
        _logger = GetLogger();

        var supportedCultureNames = GlobalizationHelpers.ExpandedLocalizedCultures
            .SelectMany(kvp => kvp.Value)
            .Select(c => c.Name)
            .ToArray();

        _app.UseRequestLocalization(new RequestLocalizationOptions()
            .AddSupportedCultures(supportedCultureNames)
            .AddSupportedUICultures(supportedCultureNames));

        WriteVersion(_logger);

        _app.Lifetime.ApplicationStarted.Register(() =>
        {
            EndpointInfo? frontendEndpointInfo = null;
            if (_frontendEndPointAccessor.Count > 0)
            {
                if (dashboardOptions.Otlp.Cors.IsCorsEnabled)
                {
                    var corsOptions = _app.Services.GetRequiredService<IOptions<CorsOptions>>().Value;

                    // Default policy allows the dashboard's origins.
                    // This is added so CORS middleware doesn't report failure for dashboard browser requests that include an origin header.
                    // Needs to be added once app is started so the resolved frontend endpoint can be used.
                    corsOptions.AddDefaultPolicy(builder =>
                    {
                        builder.WithOrigins(_frontendEndPointAccessor.Select(accessor => accessor().GetResolvedAddress()).ToArray());
                        builder.AllowAnyHeader();
                        builder.AllowAnyMethod();
                    });
                }

                frontendEndpointInfo = _frontendEndPointAccessor[0]();
                _logger.LogInformation("Now listening on: {DashboardUri}", frontendEndpointInfo.GetResolvedAddress());
            }

            if (_otlpServiceGrpcEndPointAccessor != null)
            {
                // This isn't used by dotnet watch but still useful to have for debugging
                _logger.LogInformation("OTLP/gRPC listening on: {OtlpEndpointUri}", _otlpServiceGrpcEndPointAccessor().GetResolvedAddress());
            }
            if (_otlpServiceHttpEndPointAccessor != null)
            {
                // This isn't used by dotnet watch but still useful to have for debugging
                _logger.LogInformation("OTLP/HTTP listening on: {OtlpEndpointUri}", _otlpServiceHttpEndPointAccessor().GetResolvedAddress());
            }

            if (_dashboardOptionsMonitor.CurrentValue.Otlp.AuthMode == OtlpAuthMode.Unsecured)
            {
                _logger.LogWarning("OTLP server is unsecured. Untrusted apps can send telemetry to the dashboard. For more information, visit https://go.microsoft.com/fwlink/?linkid=2267030");
            }

            // Log frontend login URL last at startup so it's easy to find in the logs.
            if (frontendEndpointInfo != null)
            {
                var options = _app.Services.GetRequiredService<IOptionsMonitor<DashboardOptions>>().CurrentValue;
                if (options.Frontend.AuthMode == FrontendAuthMode.BrowserToken)
                {
                    // DOTNET_RUNNING_IN_CONTAINER is a well-known environment variable added by official .NET images.
                    // https://learn.microsoft.com/dotnet/core/tools/dotnet-environment-variables#dotnet_running_in_container-and-dotnet_running_in_containers
                    var isContainer = _app.Configuration.GetBool("DOTNET_RUNNING_IN_CONTAINER") ?? false;

                    LoggingHelpers.WriteDashboardUrl(_logger, frontendEndpointInfo.GetResolvedAddress(replaceIPAnyWithLocalhost: true), options.Frontend.BrowserToken, isContainer);
                }
            }

            // One-off async initialization of telemetry service.
            var telemetryService = _app.Services.GetRequiredService<DashboardTelemetryService>();
            _ = Task.Run(async () =>
            {
                try
                {
                    await telemetryService.InitializeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error initializing telemetry service.");
                }
            });
        });

        // Redirect browser directly to /structuredlogs address if the dashboard is running without a resource service.
        // This is done to avoid immediately navigating in the Blazor app.
        _app.Use(async (context, next) =>
        {
            if (context.Request.Path.Equals(TargetLocationInterceptor.ResourcesPath, StringComparisons.UrlPath))
            {
                var client = context.RequestServices.GetRequiredService<IDashboardClient>();
                if (!client.IsEnabled)
                {
                    context.Response.Redirect(TargetLocationInterceptor.StructuredLogsPath);
                    return;
                }
            }

            await next(context).ConfigureAwait(false);
        });

        if (!string.IsNullOrEmpty(dashboardOptions.Otlp.Cors.AllowedOrigins))
        {
            // Only add CORS middleware when there is CORS configuration.
            // The default policy only allows the dashboard origin. Certain endpoints expose CORS for external origins, e.g. OTLP HTTP endpoints.
            _app.UseCors();
        }

        _app.UseMiddleware<ValidateTokenMiddleware>();

        // Configure the HTTP request pipeline.
        if (!_app.Environment.IsDevelopment())
        {
            _app.UseExceptionHandler("/error");
            if (isAllHttps)
            {
                _app.UseHsts();
            }
        }

        _app.UseResponseCompression();

        _app.UseStatusCodePagesWithReExecute("/error/{0}");

        if (isAllHttps)
        {
            _app.UseHttpsRedirection();
        }

        _app.UseStaticFiles(new StaticFileOptions()
        {
            OnPrepareResponse = context =>
            {
                // If Cache-Control isn't already set to something, set it to 'no-cache' so that the
                // ETag and Last-Modified headers will be respected by the browser.
                // This may be able to be removed if https://github.com/dotnet/aspnetcore/issues/44153
                // is fixed to make this the default
                if (context.Context.Response.Headers.CacheControl.Count == 0)
                {
                    context.Context.Response.Headers.CacheControl = "no-cache";
                }
            }
        });

        _app.UseAuthorization();

        _app.UseMiddleware<BrowserSecurityHeadersMiddleware>();
        _app.UseAntiforgery();

        _app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

        // OTLP HTTP services.
        _app.MapHttpOtlpApi(dashboardOptions.Otlp);

        // OTLP gRPC services.
        _app.MapGrpcService<OtlpGrpcMetricsService>();
        _app.MapGrpcService<OtlpGrpcTraceService>();
        _app.MapGrpcService<OtlpGrpcLogsService>();

        _app.MapDashboardApi(dashboardOptions);
    }

    private ILogger<DashboardWebApplication> GetLogger()
    {
        return _app.Services.GetRequiredService<ILoggerFactory>().CreateLogger<DashboardWebApplication>();
    }

    private static void WriteValidationFailures(ILogger<DashboardWebApplication> logger, IReadOnlyList<string> validationFailures)
    {
        logger.LogError("Failed to start the dashboard due to {Count} configuration error(s).", validationFailures.Count);
        foreach (var message in validationFailures)
        {
            logger.LogError("{ErrorMessage}", message);
        }
    }

    private static void WriteVersion(ILogger<DashboardWebApplication> logger)
    {
        if (typeof(DashboardWebApplication).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion is string informationalVersion)
        {
            // Write version at info level so it's written to the console by default. Help us debug user issues.
            // Display version and commit like 8.0.0-preview.2.23619.3+17dd83f67c6822954ec9a918ef2d048a78ad4697
            logger.LogInformation("Aspire version: {Version}", informationalVersion);
        }
    }

    /// <summary>
    /// Load <see cref="DashboardOptions"/> from configuration without using DI. This performs
    /// the same steps as getting the options from DI but without the need for a service provider.
    /// </summary>
    private static bool TryGetDashboardOptions(WebApplicationBuilder builder, IConfigurationSection dashboardConfigSection, [NotNullWhen(true)] out DashboardOptions? dashboardOptions, [NotNullWhen(false)] out IEnumerable<string>? failureMessages)
    {
        dashboardOptions = new DashboardOptions();
        dashboardConfigSection.Bind(dashboardOptions);
        new PostConfigureDashboardOptions(builder.Configuration).PostConfigure(name: string.Empty, dashboardOptions);
        var result = new ValidateDashboardOptions().Validate(name: string.Empty, dashboardOptions);
        if (result.Failed)
        {
            failureMessages = result.Failures;
            return false;
        }
        else
        {
            failureMessages = null;
            return true;
        }
    }

    // Kestrel endpoints are loaded from configuration. This is done so that advanced configuration of endpoints is
    // possible from the caller. e.g., using environment variables to configure each endpoint's TLS certificate.
    private void ConfigureKestrelEndpoints(WebApplicationBuilder builder, DashboardOptions dashboardOptions)
    {
        // A single endpoint is configured if URLs are the same and the port isn't dynamic.
        var frontendAddresses = dashboardOptions.Frontend.GetEndpointAddresses();
        var otlpGrpcAddress = dashboardOptions.Otlp.GetGrpcEndpointAddress();
        var otlpHttpAddress = dashboardOptions.Otlp.GetHttpEndpointAddress();
        var hasSingleEndpoint = frontendAddresses.Count == 1 && IsSameOrNull(frontendAddresses[0], otlpGrpcAddress) && IsSameOrNull(frontendAddresses[0], otlpHttpAddress);

        var initialValues = new Dictionary<string, string?>();
        var browserEndpointNames = new List<string>(capacity: frontendAddresses.Count);

        if (!hasSingleEndpoint)
        {
            // Translate high-level config settings such as ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL and ASPNETCORE_URLS
            // to Kestrel's schema for loading endpoints from configuration.
            if (otlpGrpcAddress != null)
            {
                AddEndpointConfiguration(initialValues, "OtlpGrpc", otlpGrpcAddress.ToString(), HttpProtocols.Http2, requiredClientCertificate: dashboardOptions.Otlp.AuthMode == OtlpAuthMode.ClientCertificate);
            }
            if (otlpHttpAddress != null)
            {
                AddEndpointConfiguration(initialValues, "OtlpHttp", otlpHttpAddress.ToString(), HttpProtocols.Http1AndHttp2, requiredClientCertificate: dashboardOptions.Otlp.AuthMode == OtlpAuthMode.ClientCertificate);
            }

            if (frontendAddresses.Count == 1)
            {
                browserEndpointNames.Add("Browser");
                AddEndpointConfiguration(initialValues, "Browser", frontendAddresses[0].ToString());
            }
            else
            {
                for (var i = 0; i < frontendAddresses.Count; i++)
                {
                    var name = $"Browser{i}";
                    browserEndpointNames.Add(name);
                    AddEndpointConfiguration(initialValues, name, frontendAddresses[i].ToString());
                }
            }
        }
        else
        {
            // At least one gRPC endpoint must be present.
            var url = otlpGrpcAddress?.ToString() ?? otlpHttpAddress?.ToString();
            AddEndpointConfiguration(initialValues, "OtlpGrpc", url!, HttpProtocols.Http1AndHttp2, requiredClientCertificate: dashboardOptions.Otlp.AuthMode == OtlpAuthMode.ClientCertificate);
        }

        static void AddEndpointConfiguration(Dictionary<string, string?> values, string endpointName, string url, HttpProtocols? protocols = null, bool requiredClientCertificate = false)
        {
            values[$"Kestrel:Endpoints:{endpointName}:Url"] = url;

            if (protocols != null)
            {
                values[$"Kestrel:Endpoints:{endpointName}:Protocols"] = protocols.ToString();
            }

            if (requiredClientCertificate && IsHttpsOrNull(BindingAddress.Parse(url)))
            {
                values[$"Kestrel:Endpoints:{endpointName}:ClientCertificateMode"] = ClientCertificateMode.RequireCertificate.ToString();
            }
        }

        builder.Configuration.AddInMemoryCollection(initialValues);

        // Use ConfigurationLoader to augment the endpoints that Kestrel created from configuration
        // with extra settings. e.g., UseOtlpConnection for the OTLP endpoint.
        builder.WebHost.ConfigureKestrel((context, serverOptions) =>
        {
            var logger = serverOptions.ApplicationServices.GetRequiredService<ILogger<DashboardWebApplication>>();

            var kestrelSection = context.Configuration.GetSection("Kestrel");
            var configurationLoader = serverOptions.Configure(kestrelSection);

            foreach (var browserEndpointName in browserEndpointNames)
            {
                configurationLoader.Endpoint(browserEndpointName, endpointConfiguration =>
                {
                    endpointConfiguration.ListenOptions.UseConnectionTypes([ConnectionType.Frontend]);

                    // Only the last endpoint is accessible. Tests should only need one but
                    // this will need to be improved if that changes.
                    _frontendEndPointAccessor.Add(CreateEndPointAccessor(endpointConfiguration));
                });
            }

            configurationLoader.Endpoint("OtlpGrpc", endpointConfiguration =>
            {
                var connectionTypes = new List<ConnectionType> { ConnectionType.Otlp };

                _otlpServiceGrpcEndPointAccessor ??= CreateEndPointAccessor(endpointConfiguration);
                if (hasSingleEndpoint)
                {
                    logger.LogDebug("Browser and OTLP accessible on a single endpoint.");

                    if (!endpointConfiguration.IsHttps)
                    {
                        logger.LogWarning(
                            "The dashboard is configured with a shared endpoint for browser access and the OTLP service. " +
                            "The endpoint doesn't use TLS so browser access is only possible via a TLS terminating proxy.");
                    }

                    connectionTypes.Add(ConnectionType.Frontend);
                    _frontendEndPointAccessor.Add(_otlpServiceGrpcEndPointAccessor);
                }

                endpointConfiguration.ListenOptions.UseConnectionTypes(connectionTypes.ToArray());

                if (endpointConfiguration.HttpsOptions.ClientCertificateMode == ClientCertificateMode.RequireCertificate)
                {
                    // Allow invalid certificates when creating the connection. Certificate validation is done in the auth middleware.
                    endpointConfiguration.HttpsOptions.ClientCertificateValidation = (certificate, chain, sslPolicyErrors) =>
                    {
                        return true;
                    };
                }
            });

            configurationLoader.Endpoint("OtlpHttp", endpointConfiguration =>
            {
                var connectionTypes = new List<ConnectionType> { ConnectionType.Otlp };

                _otlpServiceHttpEndPointAccessor ??= CreateEndPointAccessor(endpointConfiguration);
                if (hasSingleEndpoint)
                {
                    logger.LogDebug("Browser and OTLP accessible on a single endpoint.");

                    if (!endpointConfiguration.IsHttps)
                    {
                        logger.LogWarning(
                            "The dashboard is configured with a shared endpoint for browser access and the OTLP service. " +
                            "The endpoint doesn't use TLS so browser access is only possible via a TLS terminating proxy.");
                    }

                    connectionTypes.Add(ConnectionType.Frontend);
                    _frontendEndPointAccessor.Add(_otlpServiceHttpEndPointAccessor);
                }

                endpointConfiguration.ListenOptions.UseConnectionTypes(connectionTypes.ToArray());

                if (endpointConfiguration.HttpsOptions.ClientCertificateMode == ClientCertificateMode.RequireCertificate)
                {
                    // Allow invalid certificates when creating the connection. Certificate validation is done in the auth middleware.
                    endpointConfiguration.HttpsOptions.ClientCertificateValidation = (certificate, chain, sslPolicyErrors) => { return true; };
                }
            });
        });

        static Func<EndpointInfo> CreateEndPointAccessor(EndpointConfiguration endpointConfiguration)
        {
            // We want to provide a way for someone to get the IP address of an endpoint.
            // However, if a dynamic port is used, the port is not known until the server is started.
            // Instead of returning the ListenOption's endpoint directly, we provide a func that returns the endpoint.
            // The endpoint on ListenOptions is updated after binding, so accessing it via the func after the server
            // has started returns the resolved port.
            var address = BindingAddress.Parse(endpointConfiguration.ConfigSection["Url"]!);
            return () =>
            {
                var endpoint = endpointConfiguration.ListenOptions.IPEndPoint!;

                return new EndpointInfo(address, endpoint, endpointConfiguration.IsHttps);
            };
        }
    }

    private static bool IsSameOrNull(BindingAddress frontendAddress, BindingAddress? otlpAddress)
    {
        return otlpAddress == null || (frontendAddress.Equals(otlpAddress) && otlpAddress.Port != 0);
    }

    private static void ConfigureAuthentication(WebApplicationBuilder builder, DashboardOptions dashboardOptions)
    {
        var authentication = builder.Services
            .AddAuthentication(o => o.DefaultScheme = ConfigureDefaultAuthScheme(dashboardOptions))
            .AddScheme<FrontendCompositeAuthenticationHandlerOptions, FrontendCompositeAuthenticationHandler>(FrontendCompositeAuthenticationDefaults.AuthenticationScheme, o => { })
            .AddScheme<OtlpCompositeAuthenticationHandlerOptions, OtlpCompositeAuthenticationHandler>(OtlpCompositeAuthenticationDefaults.AuthenticationScheme, o => { })
            .AddScheme<OtlpApiKeyAuthenticationHandlerOptions, OtlpApiKeyAuthenticationHandler>(OtlpApiKeyAuthenticationDefaults.AuthenticationScheme, o => { })
            .AddScheme<ConnectionTypeAuthenticationHandlerOptions, ConnectionTypeAuthenticationHandler>(ConnectionTypeAuthenticationDefaults.AuthenticationSchemeFrontend, o => o.RequiredConnectionType = ConnectionType.Frontend)
            .AddScheme<ConnectionTypeAuthenticationHandlerOptions, ConnectionTypeAuthenticationHandler>(ConnectionTypeAuthenticationDefaults.AuthenticationSchemeOtlp, o => o.RequiredConnectionType = ConnectionType.Otlp)
            .AddCertificate(options =>
            {
                // Bind options to configuration so they can be overridden by environment variables.
                builder.Configuration.Bind("Dashboard:Otlp:CertificateAuthOptions", options);

                options.Events = new CertificateAuthenticationEvents
                {
                    OnCertificateValidated = context =>
                    {
                        var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<DashboardOptions>>().Value;
                        if (options.Otlp.AllowedCertificates is { Count: > 0 } allowList)
                        {
                            string? certThumbprint = null;

                            var allowed = false;
                            foreach (var rule in allowList)
                            {
                                certThumbprint ??= context.ClientCertificate.GetCertHashString(HashAlgorithmName.SHA256);

                                // Thumbprint is hexadecimal and is case-insensitive.
                                if (string.Equals(rule.Thumbprint, certThumbprint, StringComparison.OrdinalIgnoreCase))
                                {
                                    allowed = true;
                                    break;
                                }
                            }

                            if (!allowed)
                            {
                                context.Fail("Certificate doesn't match allow list.");
                                return Task.CompletedTask;
                            }
                        }

                        var claims = new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier,
                                context.ClientCertificate.Subject,
                                ClaimValueTypes.String, context.Options.ClaimsIssuer),
                            new Claim(ClaimTypes.Name,
                                context.ClientCertificate.Subject,
                                ClaimValueTypes.String, context.Options.ClaimsIssuer)
                        };

                        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));
                        context.Success();

                        return Task.CompletedTask;
                    }
                };
            });

        switch (dashboardOptions.Frontend.AuthMode)
        {
            case FrontendAuthMode.OpenIdConnect:
                authentication.AddPolicyScheme(FrontendAuthenticationDefaults.AuthenticationSchemeOpenIdConnect, displayName: FrontendAuthenticationDefaults.AuthenticationSchemeOpenIdConnect, o =>
                {
                    // The frontend authentication scheme just redirects to OpenIdConnect and Cookie schemes, as appropriate.
                    o.ForwardDefault = CookieAuthenticationDefaults.AuthenticationScheme;
                    o.ForwardChallenge = OpenIdConnectDefaults.AuthenticationScheme;
                });

                authentication.AddCookie(options =>
                {
                    options.Cookie.Name = DashboardAuthCookieName;
                });

                authentication.AddOpenIdConnect(options =>
                {
                    // Use authorization code flow so clients don't see access tokens.
                    options.ResponseType = OpenIdConnectResponseType.Code;

                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                    // Scopes "openid" and "profile" are added by default, but need to be re-added
                    // in case configuration exists for Authentication:Schemes:OpenIdConnect:Scope.
                    if (!options.Scope.Contains(OpenIdConnectScope.OpenId))
                    {
                        options.Scope.Add(OpenIdConnectScope.OpenId);
                    }

                    if (!options.Scope.Contains("profile"))
                    {
                        options.Scope.Add("profile");
                    }

                    // Redirect to resources upon sign-in.
                    options.CallbackPath = TargetLocationInterceptor.ResourcesPath;

                    // Avoid "message.State is null or empty" due to use of CallbackPath above.
                    options.SkipUnrecognizedRequests = true;
                });
                break;
            case FrontendAuthMode.BrowserToken:
                authentication.AddPolicyScheme(FrontendAuthenticationDefaults.AuthenticationSchemeBrowserToken, displayName: FrontendAuthenticationDefaults.AuthenticationSchemeBrowserToken, o =>
                {
                    o.ForwardDefault = CookieAuthenticationDefaults.AuthenticationScheme;
                });

                authentication.AddCookie(options =>
                {
                    options.LoginPath = "/login";
                    options.ReturnUrlParameter = "returnUrl";
                    options.ExpireTimeSpan = TimeSpan.FromDays(3);
                    options.Events.OnSigningIn = context =>
                    {
                        // Add claim when signing in with cookies from browser token.
                        // Authorization requires this claim. This prevents an identity from another auth scheme from being allow.
                        var claimsIdentity = (ClaimsIdentity)context.Principal!.Identity!;
                        claimsIdentity.AddClaim(new Claim(FrontendAuthorizationDefaults.BrowserTokenClaimName, bool.TrueString));
                        return Task.CompletedTask;
                    };
                    options.Cookie.Name = DashboardAuthCookieName;
                });
                break;
            case FrontendAuthMode.Unsecured:
                authentication.AddScheme<AuthenticationSchemeOptions, UnsecuredAuthenticationHandler>(FrontendAuthenticationDefaults.AuthenticationSchemeUnsecured, o => { });
                break;
        }

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(
                name: OtlpAuthorization.PolicyName,
                policy: new AuthorizationPolicyBuilder(OtlpCompositeAuthenticationDefaults.AuthenticationScheme)
                    .RequireClaim(OtlpAuthorization.OtlpClaimName, [bool.TrueString])
                    .Build());

            switch (dashboardOptions.Frontend.AuthMode)
            {
                case FrontendAuthMode.OpenIdConnect:
                    options.AddPolicy(
                        name: FrontendAuthorizationDefaults.PolicyName,
                        policy: new AuthorizationPolicyBuilder(FrontendCompositeAuthenticationDefaults.AuthenticationScheme)
                            .RequireOpenIdClaims(options: dashboardOptions.Frontend.OpenIdConnect)
                            .Build());
                    break;
                case FrontendAuthMode.BrowserToken:
                    options.AddPolicy(
                        name: FrontendAuthorizationDefaults.PolicyName,
                        policy: new AuthorizationPolicyBuilder(FrontendCompositeAuthenticationDefaults.AuthenticationScheme)
                            .RequireClaim(FrontendAuthorizationDefaults.BrowserTokenClaimName)
                            .Build());
                    break;
                case FrontendAuthMode.Unsecured:
                    options.AddPolicy(
                        name: FrontendAuthorizationDefaults.PolicyName,
                        policy: new AuthorizationPolicyBuilder(FrontendCompositeAuthenticationDefaults.AuthenticationScheme)
                            .RequireClaim(FrontendAuthorizationDefaults.UnsecuredClaimName)
                            .Build());
                    break;
                default:
                    throw new NotSupportedException($"Unexpected {nameof(FrontendAuthMode)} enum member: {dashboardOptions.Frontend.AuthMode}");
            }
        });

        // ASP.NET Core authentication needs to have the correct default scheme for the configured frontend auth.
        // This is required for ASP.NET Core/SignalR/Blazor to flow the authenticated user from the request and into the dashboard app.
        static string ConfigureDefaultAuthScheme(DashboardOptions dashboardOptions)
        {
            return dashboardOptions.Frontend.AuthMode switch
            {
                FrontendAuthMode.Unsecured => FrontendAuthenticationDefaults.AuthenticationSchemeUnsecured,
                _ => CookieAuthenticationDefaults.AuthenticationScheme
            };
        }
    }

    public int Run()
    {
        if (_validationFailures.Count > 0)
        {
            return -1;
        }

        _app.Run();
        return 0;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        Debug.Assert(_validationFailures.Count == 0, "Validation failures: " + Environment.NewLine + string.Join(Environment.NewLine, _validationFailures));
        return _app.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        Debug.Assert(_validationFailures.Count == 0, "Validation failures: " + Environment.NewLine + string.Join(Environment.NewLine, _validationFailures));
        return _app.StopAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _app.DisposeAsync();
    }

    private static bool IsHttpsOrNull(BindingAddress? address) => address == null || string.Equals(address.Scheme, "https", StringComparison.Ordinal);
}

public record EndpointInfo(BindingAddress BindingAddress, IPEndPoint EndPoint, bool IsHttps)
{
    public string GetResolvedAddress(bool replaceIPAnyWithLocalhost = false)
    {
        if (!IsAnyIPHost(BindingAddress.Host))
        {
            return BindingAddress.Scheme.ToLowerInvariant() + Uri.SchemeDelimiter + BindingAddress.Host.ToLowerInvariant() + ":" + EndPoint.Port.ToString(CultureInfo.InvariantCulture);
        }

        if (replaceIPAnyWithLocalhost)
        {
            // Clicking on an any IP host link, e.g. http://0.0.0.0:1234, doesn't work.
            // Instead, write localhost so the link at least has a chance to work when the container and browser are on the same machine.
            return BindingAddress.Scheme.ToLowerInvariant() + Uri.SchemeDelimiter + "localhost:" + EndPoint.Port.ToString(CultureInfo.InvariantCulture);
        }

        return BindingAddress.Scheme.ToLowerInvariant() + Uri.SchemeDelimiter + EndPoint.ToString();

        static bool IsAnyIPHost(string host)
        {
            // It's ok to use IPAddress.ToString here because the string is cached inside IPAddress.
            return host == "*" || host == "+" || host == IPAddress.Any.ToString() || host == IPAddress.IPv6Any.ToString();
        }
    }
}

public static class FrontendAuthorizationDefaults
{
    public const string PolicyName = "Frontend";
    public const string BrowserTokenClaimName = "BrowserTokenClaim";
    public const string UnsecuredClaimName = "UnsecuredTokenClaim";
}

public static class FrontendAuthenticationDefaults
{
    public const string AuthenticationSchemeOpenIdConnect = "FrontendOpenIdConnect";
    public const string AuthenticationSchemeBrowserToken = "FrontendBrowserToken";
    public const string AuthenticationSchemeUnsecured = "FrontendUnsecured";
}
