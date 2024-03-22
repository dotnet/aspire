// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Reflection;
using System.Security.Claims;
using Aspire.Dashboard.Authentication;
using Aspire.Dashboard.Authentication.OtlpApiKey;
using Aspire.Dashboard.Authentication.OtlpConnection;
using Aspire.Dashboard.Components;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Grpc;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard;

public class DashboardWebApplication : IAsyncDisposable
{
    internal const string DashboardInsecureAllowAnonymousVariableName = "DOTNET_DASHBOARD_INSECURE_ALLOW_ANONYMOUS";
    internal const string DashboardOtlpAuthModeVariableName = "DOTNET_DASHBOARD_OTLP_AUTH_MODE";
    internal const string DashboardOtlpApiKeyVariableName = "DOTNET_DASHBOARD_OTLP_API_KEY";
    internal const string DashboardOtlpUrlVariableName = "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL";
    internal const string DashboardOtlpUrlDefaultValue = "http://localhost:18889";
    internal const string DashboardUrlVariableName = "ASPNETCORE_URLS";
    internal const string DashboardUrlDefaultValue = "http://localhost:18888";

    private readonly WebApplication _app;
    private Func<EndpointInfo>? _browserEndPointAccessor;
    private Func<EndpointInfo>? _otlpServiceEndPointAccessor;

    public Func<EndpointInfo> BrowserEndPointAccessor
    {
        get => _browserEndPointAccessor ?? throw new InvalidOperationException("WebApplication not started yet.");
    }

    public Func<EndpointInfo> OtlpServiceEndPointAccessor
    {
        get => _otlpServiceEndPointAccessor ?? throw new InvalidOperationException("WebApplication not started yet.");
    }

    /// <summary>
    /// Create a new instance of the <see cref="DashboardWebApplication"/> class.
    /// </summary>
    /// <param name="configureBuilder">Configuration the internal app builder. This is for unit testing.</param>
    public DashboardWebApplication(Action<WebApplicationBuilder>? configureBuilder = null)
    {
        var builder = WebApplication.CreateBuilder();
        if (configureBuilder != null)
        {
            configureBuilder(builder);
        }

#if !DEBUG
        builder.Logging.AddFilter("Default", LogLevel.Information);
        builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.None);
        builder.Logging.AddFilter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Error);
#endif

        var dashboardConfig = LoadDashboardConfig(builder.Configuration);

        ConfigureKestrelEndpoints(builder, dashboardConfig);

        var browserHttpsPort = dashboardConfig.BrowserUris.FirstOrDefault(IsHttps)?.Port;
        var isAllHttps = browserHttpsPort is not null && IsHttps(dashboardConfig.OtlpUri);
        if (isAllHttps)
        {
            // Explicitly configure the HTTPS redirect port as we're possibly listening on multiple HTTPS addresses
            // if the dashboard OTLP URL is configured to use HTTPS too
            builder.Services.Configure<HttpsRedirectionOptions>(options => options.HttpsPort = browserHttpsPort);
        }

        ConfigureAuthentication(builder, dashboardConfig);

        // Add services to the container.
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();

        // Data from the server.
        builder.Services.AddScoped<IDashboardClient, DashboardClient>();

        // OTLP services.
        builder.Services.AddGrpc();
        builder.Services.AddSingleton<TelemetryRepository>();
        builder.Services.AddTransient<StructuredLogsViewModel>();
        builder.Services.AddTransient<TracesViewModel>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IOutgoingPeerResolver, ResourceOutgoingPeerResolver>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IOutgoingPeerResolver, BrowserLinkOutgoingPeerResolver>());

        builder.Services.AddFluentUIComponents();

        builder.Services.AddScoped<ThemeManager>();
        // ShortcutManager is scoped because we want shortcuts to apply one browser window.
        builder.Services.AddScoped<ShortcutManager>();
        builder.Services.AddSingleton<IInstrumentUnitResolver, DefaultInstrumentUnitResolver>();

        // Time zone is set by the browser.
        builder.Services.AddScoped<BrowserTimeProvider>();

        builder.Services.AddLocalization();

        _app = builder.Build();

        var logger = _app.Services.GetRequiredService<ILoggerFactory>().CreateLogger<DashboardWebApplication>();

        // this needs to be explicitly enumerated for each supported language
        // our language list comes from https://github.com/dotnet/arcade/blob/89008f339a79931cc49c739e9dbc1a27c608b379/src/Microsoft.DotNet.XliffTasks/build/Microsoft.DotNet.XliffTasks.props#L22
        var supportedLanguages = new[]
        {
            "en", "cs", "de", "es", "fr", "it", "ja", "ko", "pl", "pt-BR", "ru", "tr", "zh-Hans", "zh-Hant"
        };

        _app.UseRequestLocalization(new RequestLocalizationOptions()
            .AddSupportedCultures(supportedLanguages)
            .AddSupportedUICultures(supportedLanguages));

        if (GetType().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion is string informationalVersion)
        {
            // Write version at info level so it's written to the console by default. Help us debug user issues.
            // Display version and commit like 8.0.0-preview.2.23619.3+17dd83f67c6822954ec9a918ef2d048a78ad4697
            logger.LogInformation("Aspire version: {Version}", informationalVersion);
        }

        _app.Lifetime.ApplicationStarted.Register(() =>
        {
            if (_browserEndPointAccessor != null)
            {
                // dotnet watch needs the trailing slash removed. See https://github.com/dotnet/sdk/issues/36709
                logger.LogInformation("Now listening on: {DashboardUri}", GetEndpointUrl(_browserEndPointAccessor()));
            }

            if (_otlpServiceEndPointAccessor != null)
            {
                // This isn't used by dotnet watch but still useful to have for debugging
                logger.LogInformation("OTLP server running at: {OtlpEndpointUri}", GetEndpointUrl(_otlpServiceEndPointAccessor()));
            }

            static string GetEndpointUrl(EndpointInfo info) => $"{(info.isHttps ? "https" : "http")}://{info.EndPoint}";
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

        // Configure the HTTP request pipeline.
        if (_app.Environment.IsDevelopment())
        {
            _app.UseDeveloperExceptionPage();
            //_app.UseBrowserLink();
        }
        else
        {
            _app.UseExceptionHandler("/Error");
            //_app.UseHsts();
        }

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

        _app.UseAntiforgery();

        _app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

        // OTLP gRPC services.
        _app.MapGrpcService<OtlpMetricsService>();
        _app.MapGrpcService<OtlpTraceService>();
        _app.MapGrpcService<OtlpLogsService>();
    }

    // Kestrel endpoints are loaded from configuration. This is done so that advanced configuration of endpoints is
    // possible from the caller. e.g., using environment variables to configure each endpoint's TLS certificate.
    private void ConfigureKestrelEndpoints(WebApplicationBuilder builder, DashboardStartupConfiguration dashboardStartupConfig)
    {
        // A single endpoint is configured if URLs are the same and the port isn't dynamic.
        var singleEndpoint = dashboardStartupConfig.BrowserUris.Length == 1 && dashboardStartupConfig.BrowserUris[0] == dashboardStartupConfig.OtlpUri && dashboardStartupConfig.OtlpUri.Port != 0;

        var initialValues = new Dictionary<string, string?>();
        var browserEndpointNames = new List<string>(capacity: dashboardStartupConfig.BrowserUris.Length);

        if (!singleEndpoint)
        {
            // Translate high-level config settings such as DOTNET_DASHBOARD_OTLP_ENDPOINT_URL and ASPNETCORE_URLS
            // to Kestrel's schema for loading endpoints from configuration.
            AddEndpointConfiguration(initialValues, "Otlp", dashboardStartupConfig.OtlpUri.OriginalString, HttpProtocols.Http2, requiredClientCertificate: dashboardStartupConfig.OtlpAuthMode == OtlpAuthMode.ClientCertificate);
            if (dashboardStartupConfig.BrowserUris.Length == 1)
            {
                browserEndpointNames.Add("Browser");
                AddEndpointConfiguration(initialValues, "Browser", dashboardStartupConfig.BrowserUris[0].OriginalString);
            }
            else
            {
                for (var i = 0; i < dashboardStartupConfig.BrowserUris.Length; i++)
                {
                    var name = $"Browser{i}";
                    browserEndpointNames.Add(name);
                    AddEndpointConfiguration(initialValues, name, dashboardStartupConfig.BrowserUris[i].OriginalString);
                }
            }
        }
        else
        {
            AddEndpointConfiguration(initialValues, "Otlp", dashboardStartupConfig.OtlpUri.OriginalString, HttpProtocols.Http1AndHttp2, requiredClientCertificate: dashboardStartupConfig.OtlpAuthMode == OtlpAuthMode.ClientCertificate);
        }

        static void AddEndpointConfiguration(Dictionary<string, string?> values, string endpointName, string url, HttpProtocols? protocols = null, bool requiredClientCertificate = false)
        {
            values[$"Kestrel:Endpoints:{endpointName}:Url"] = url;

            if (protocols != null)
            {
                values[$"Kestrel:Endpoints:{endpointName}:Protocols"] = protocols.ToString();
            }

            if (requiredClientCertificate)
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
                    // Only the last endpoint is accessible. Tests should only need one but
                    // this will need to be improved if that changes.
                    _browserEndPointAccessor = CreateEndPointAccessor(endpointConfiguration.ListenOptions, endpointConfiguration.IsHttps);
                });
            }

            configurationLoader.Endpoint("Otlp", endpointConfiguration =>
            {
                _otlpServiceEndPointAccessor = CreateEndPointAccessor(endpointConfiguration.ListenOptions, endpointConfiguration.IsHttps);
                if (singleEndpoint)
                {
                    logger.LogDebug("Browser and OTLP accessible on a single endpoint.");

                    if (!endpointConfiguration.IsHttps)
                    {
                        logger.LogWarning(
                            "The dashboard is configured with a shared endpoint for browser access and the OTLP service. " +
                            "The endpoint doesn't use TLS so browser access is only possible via a TLS terminating proxy.");
                    }

                    _browserEndPointAccessor = _otlpServiceEndPointAccessor;
                }

                endpointConfiguration.ListenOptions.UseOtlpConnection();

                if (endpointConfiguration.HttpsOptions.ClientCertificateMode == ClientCertificateMode.RequireCertificate)
                {
                    // Allow invalid certificates when creating the connection. Certificate validation is done in the auth middleware.
                    endpointConfiguration.HttpsOptions.ClientCertificateValidation = (certificate, chain, sslPolicyErrors) =>
                    {
                        return true;
                    };
                }
            });
        });

        static Func<EndpointInfo> CreateEndPointAccessor(ListenOptions options, bool isHttps)
        {
            // We want to provide a way for someone to get the IP address of an endpoint.
            // However, if a dynamic port is used, the port is not known until the server is started.
            // Instead of returning the ListenOption's endpoint directly, we provide a func that returns the endpoint.
            // The endpoint on ListenOptions is updated after binding, so accessing it via the func after the server
            // has started returns the resolved port.
            return () => new EndpointInfo(options.IPEndPoint!, isHttps);
        }
    }

    private static void ConfigureAuthentication(WebApplicationBuilder builder, DashboardStartupConfiguration dashboardStartupConfig)
    {
        builder.Services
            .AddAuthentication(defaultScheme: OtlpCompositeAuthenticationDefaults.AuthenticationScheme)
            .AddScheme<OtlpCompositeAuthenticationHandlerOptions, OtlpCompositeAuthenticationHandler>(OtlpCompositeAuthenticationDefaults.AuthenticationScheme, o =>
            {
                o.OtlpAuthMode = dashboardStartupConfig.OtlpAuthMode;
            })
            .AddScheme<OtlpApiKeyAuthenticationHandlerOptions, OtlpApiKeyAuthenticationHandler>(OtlpApiKeyAuthenticationDefaults.AuthenticationScheme, o =>
            {
                o.OtlpApiKey = dashboardStartupConfig.OtlpApiKey;
            })
            .AddScheme<OtlpConnectionAuthenticationHandlerOptions, OtlpConnectionAuthenticationHandler>(OtlpConnectionAuthenticationDefaults.AuthenticationScheme, o => { })
            .AddCertificate(options =>
            {
                // Bind options to configuration so they can be override by environment variables.
                builder.Configuration.Bind("CertificateAuthentication", options);

                options.Events = new CertificateAuthenticationEvents
                {
                    OnCertificateValidated = context =>
                    {
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

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(OtlpAuthorization.PolicyName, policy => policy.RequireClaim(OtlpAuthorization.OtlpClaimName));
        });
    }

    public void Run() => _app.Run();

    public Task StartAsync(CancellationToken cancellationToken = default) => _app.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken = default) => _app.StopAsync(cancellationToken);

    public ValueTask DisposeAsync()
    {
        return _app.DisposeAsync();
    }

    private static bool IsHttps(Uri uri) => string.Equals(uri.Scheme, "https", StringComparison.Ordinal);

    private static DashboardStartupConfiguration LoadDashboardConfig(IConfiguration configuration)
    {
        var browserUris = configuration.GetUris(DashboardUrlVariableName, new(DashboardUrlDefaultValue));
        if (browserUris.Length == 0)
        {
            throw new InvalidOperationException($"One URL for Aspire dashboard OTLP endpoint with {DashboardUrlDefaultValue} is required.");
        }

        var otlpUris = configuration.GetUris(DashboardOtlpUrlVariableName, new(DashboardOtlpUrlDefaultValue));
        if (otlpUris.Length != 1)
        {
            throw new InvalidOperationException($"At least one URL for Aspire dashboard browser endpoint with {DashboardOtlpUrlDefaultValue} is required.");
        }

        var allowAnonymous = configuration.GetBool(DashboardInsecureAllowAnonymousVariableName) ?? false;
        var otlpAuthMode = configuration.GetEnum<OtlpAuthMode>(DashboardOtlpAuthModeVariableName);
        string? otlpApiKey = null;

        switch (otlpAuthMode)
        {
            case null:
                if (!allowAnonymous)
                {
                    throw new InvalidOperationException($"Configuration of OTLP endpoint authentication is required. Either specify {DashboardInsecureAllowAnonymousVariableName} with a value of true, or specify {DashboardOtlpAuthModeVariableName}. Possible values: {string.Join(", ", typeof(OtlpAuthMode).GetEnumNames())}");
                }
                otlpAuthMode = OtlpAuthMode.None;
                break;
            case OtlpAuthMode.None:
                break;
            case OtlpAuthMode.ApiKey:
                otlpApiKey = configuration[DashboardOtlpApiKeyVariableName];
                if (string.IsNullOrEmpty(otlpApiKey))
                {
                    throw new InvalidOperationException($"{DashboardOtlpAuthModeVariableName} value of {nameof(OtlpAuthMode.ApiKey)} requires an API key from {DashboardOtlpApiKeyVariableName}.");
                }
                break;
            case OtlpAuthMode.ClientCertificate:
                if (!IsHttps(otlpUris[0]))
                {
                    throw new InvalidOperationException($"{DashboardOtlpAuthModeVariableName} value of {nameof(OtlpAuthMode.ClientCertificate)} requires a HTTPS OTLP endpoint.");
                }
                break;
            default:
                throw new InvalidOperationException($"Unexpected auth mode value: {otlpAuthMode}");
        }

        return new DashboardStartupConfiguration
        {
            BrowserUris = browserUris,
            OtlpUri = otlpUris[0],
            OtlpAuthMode = otlpAuthMode.Value,
            OtlpApiKey = otlpApiKey
        };
    }
}

public record EndpointInfo(IPEndPoint EndPoint, bool isHttps);
