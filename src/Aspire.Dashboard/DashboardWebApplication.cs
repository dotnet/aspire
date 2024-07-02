// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using Aspire.Dashboard.Authentication;
using Aspire.Dashboard.Authentication.OpenIdConnect;
using Aspire.Dashboard.Authentication.OtlpApiKey;
using Aspire.Dashboard.Authentication.OtlpConnection;
using Aspire.Dashboard.Components;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp;
using Aspire.Dashboard.Otlp.Grpc;
using Aspire.Dashboard.Otlp.Http;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Hosting;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
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
    private Func<EndpointInfo>? _frontendEndPointAccessor;
    private Func<EndpointInfo>? _otlpServiceGrpcEndPointAccessor;
    private Func<EndpointInfo>? _otlpServiceHttpEndPointAccessor;

    public Func<EndpointInfo> FrontendEndPointAccessor
    {
        get => _frontendEndPointAccessor ?? throw new InvalidOperationException("WebApplication not started yet.");
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

    /// <summary>
    /// Create a new instance of the <see cref="DashboardWebApplication"/> class.
    /// </summary>
    /// <param name="configureBuilder">Configuration the internal app builder. This is for unit testing.</param>
    public DashboardWebApplication(Action<WebApplicationBuilder>? configureBuilder = null)
    {
        var builder = WebApplication.CreateBuilder();

        configureBuilder?.Invoke(builder);

#if !DEBUG
        builder.Logging.AddFilter("Default", LogLevel.Information);
        builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.None);
        builder.Logging.AddFilter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Error);
#endif

        // Allow for a user specified JSON config file on disk. Throw an error if the specified file doesn't exist.
        if (builder.Configuration[DashboardConfigNames.DashboardConfigFilePathName.ConfigKey] is { Length: > 0 } configFilePath)
        {
            builder.Configuration.AddJsonFile(configFilePath, optional: false, reloadOnChange: true);
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
            WriteVersion(_logger);
            WriteValidationFailures(_logger, _validationFailures);
            return;
        }
        else
        {
            _validationFailures = Array.Empty<string>();
        }

        ConfigureKestrelEndpoints(builder, dashboardOptions);

        var browserHttpsPort = dashboardOptions.Frontend.GetEndpointUris().FirstOrDefault(IsHttpsOrNull)?.Port;
        var isAllHttps = browserHttpsPort is not null && IsHttpsOrNull(dashboardOptions.Otlp.GetGrpcEndpointUri()) && IsHttpsOrNull(dashboardOptions.Otlp.GetHttpEndpointUri());
        if (isAllHttps)
        {
            // Explicitly configure the HTTPS redirect port as we're possibly listening on multiple HTTPS addresses
            // if the dashboard OTLP URL is configured to use HTTPS too
            builder.Services.Configure<HttpsRedirectionOptions>(options => options.HttpsPort = browserHttpsPort);
        }

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

        // Data from the server.
        builder.Services.AddScoped<IDashboardClient, DashboardClient>();

        // OTLP services.
        builder.Services.AddGrpc();
        builder.Services.AddSingleton<TelemetryRepository>();
        builder.Services.AddTransient<StructuredLogsViewModel>();

        builder.Services.AddTransient<OtlpLogsService>();
        builder.Services.AddTransient<OtlpTraceService>();
        builder.Services.AddTransient<OtlpMetricsService>();

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

        builder.Services.AddAntiforgery(options =>
        {
            options.Cookie.Name = DashboardAntiForgeryCookieName;
        });

        _app = builder.Build();

        _dashboardOptionsMonitor = _app.Services.GetRequiredService<IOptionsMonitor<DashboardOptions>>();

        _logger = GetLogger();

        // this needs to be explicitly enumerated for each supported language
        // our language list comes from https://github.com/dotnet/arcade/blob/89008f339a79931cc49c739e9dbc1a27c608b379/src/Microsoft.DotNet.XliffTasks/build/Microsoft.DotNet.XliffTasks.props#L22
        var supportedLanguages = new[]
        {
            "en", "cs", "de", "es", "fr", "it", "ja", "ko", "pl", "pt-BR", "ru", "tr", "zh-Hans", "zh-Hant", // Standard cultures for compliance.
            "zh-CN" // Non-standard culture but it is the default in many Chinese browsers. Adding zh-CN allows OS culture customization to flow through the dashboard.
        };

        _app.UseRequestLocalization(new RequestLocalizationOptions()
            .AddSupportedCultures(supportedLanguages)
            .AddSupportedUICultures(supportedLanguages));

        WriteVersion(_logger);

        _app.Lifetime.ApplicationStarted.Register(() =>
        {
            if (_frontendEndPointAccessor != null)
            {
                var url = _frontendEndPointAccessor().Address;
                _logger.LogInformation("Now listening on: {DashboardUri}", url);

                var options = _app.Services.GetRequiredService<IOptionsMonitor<DashboardOptions>>().CurrentValue;
                if (options.Frontend.AuthMode == FrontendAuthMode.BrowserToken)
                {
                    LoggingHelpers.WriteDashboardUrl(_logger, url, options.Frontend.BrowserToken);
                }
            }

            if (_otlpServiceGrpcEndPointAccessor != null)
            {
                // This isn't used by dotnet watch but still useful to have for debugging
                _logger.LogInformation("OTLP/gRPC listening on: {OtlpEndpointUri}", _otlpServiceGrpcEndPointAccessor().Address);
            }
            if (_otlpServiceHttpEndPointAccessor != null)
            {
                // This isn't used by dotnet watch but still useful to have for debugging
                _logger.LogInformation("OTLP/HTTP listening on: {OtlpEndpointUri}", _otlpServiceHttpEndPointAccessor().Address);
            }

            if (_dashboardOptionsMonitor.CurrentValue.Otlp.AuthMode == OtlpAuthMode.Unsecured)
            {
                _logger.LogWarning("OTLP server is unsecured. Untrusted apps can send telemetry to the dashboard. For more information, visit https://go.microsoft.com/fwlink/?linkid=2267030");
            }
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
        var httpEndpoint = dashboardOptions.Otlp.GetHttpEndpointUri();
        if (httpEndpoint != null)
        {
            _app.MapHttpOtlpApi();
        }

        // OTLP gRPC services.
        _app.MapGrpcService<OtlpGrpcMetricsService>();
        _app.MapGrpcService<OtlpGrpcTraceService>();
        _app.MapGrpcService<OtlpGrpcLogsService>();

        if (dashboardOptions.Frontend.AuthMode == FrontendAuthMode.BrowserToken)
        {
            _app.MapPost("/api/validatetoken", async (string token, HttpContext httpContext, IOptionsMonitor<DashboardOptions> dashboardOptions) =>
            {
                return await ValidateTokenMiddleware.TryAuthenticateAsync(token, httpContext, dashboardOptions).ConfigureAwait(false);
            });

#if DEBUG
            // Available in local debug for testing.
            _app.MapGet("/api/signout", async (HttpContext httpContext) =>
            {
                await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(
                    httpContext,
                    CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
                httpContext.Response.Redirect("/");
            });
#endif
        }
        else if (dashboardOptions.Frontend.AuthMode == FrontendAuthMode.OpenIdConnect)
        {
            _app.MapPost("/authentication/logout", () => TypedResults.SignOut(authenticationSchemes: [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]));
        }
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
        var frontendUris = dashboardOptions.Frontend.GetEndpointUris();
        var otlpGrpcUri = dashboardOptions.Otlp.GetGrpcEndpointUri();
        var otlpHttpUri = dashboardOptions.Otlp.GetHttpEndpointUri();
        var hasSingleEndpoint = frontendUris.Count == 1 && IsSameOrNull(frontendUris[0], otlpGrpcUri) && IsSameOrNull(frontendUris[0], otlpHttpUri);

        var initialValues = new Dictionary<string, string?>();
        var browserEndpointNames = new List<string>(capacity: frontendUris.Count);

        if (!hasSingleEndpoint)
        {
            // Translate high-level config settings such as DOTNET_DASHBOARD_OTLP_ENDPOINT_URL and ASPNETCORE_URLS
            // to Kestrel's schema for loading endpoints from configuration.
            if (otlpGrpcUri != null)
            {
                AddEndpointConfiguration(initialValues, "OtlpGrpc", otlpGrpcUri.OriginalString, HttpProtocols.Http2, requiredClientCertificate: dashboardOptions.Otlp.AuthMode == OtlpAuthMode.ClientCertificate);
            }
            if (otlpHttpUri != null)
            {
                AddEndpointConfiguration(initialValues, "OtlpHttp", otlpHttpUri.OriginalString, HttpProtocols.Http1AndHttp2, requiredClientCertificate: dashboardOptions.Otlp.AuthMode == OtlpAuthMode.ClientCertificate);
            }

            if (frontendUris.Count == 1)
            {
                browserEndpointNames.Add("Browser");
                AddEndpointConfiguration(initialValues, "Browser", frontendUris[0].OriginalString);
            }
            else
            {
                for (var i = 0; i < frontendUris.Count; i++)
                {
                    var name = $"Browser{i}";
                    browserEndpointNames.Add(name);
                    AddEndpointConfiguration(initialValues, name, frontendUris[i].OriginalString);
                }
            }
        }
        else
        {
            // At least one gRPC endpoint must be present.
            var url = otlpGrpcUri?.OriginalString ?? otlpHttpUri?.OriginalString;
            AddEndpointConfiguration(initialValues, "OtlpGrpc", url!, HttpProtocols.Http1AndHttp2, requiredClientCertificate: dashboardOptions.Otlp.AuthMode == OtlpAuthMode.ClientCertificate);
        }

        static void AddEndpointConfiguration(Dictionary<string, string?> values, string endpointName, string url, HttpProtocols? protocols = null, bool requiredClientCertificate = false)
        {
            values[$"Kestrel:Endpoints:{endpointName}:Url"] = url;

            if (protocols != null)
            {
                values[$"Kestrel:Endpoints:{endpointName}:Protocols"] = protocols.ToString();
            }

            if (requiredClientCertificate && IsHttpsOrNull(new Uri(url)))
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
                    _frontendEndPointAccessor ??= CreateEndPointAccessor(endpointConfiguration);
                });
            }

            configurationLoader.Endpoint("OtlpGrpc", endpointConfiguration =>
            {
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

                    _frontendEndPointAccessor = _otlpServiceGrpcEndPointAccessor;
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

            configurationLoader.Endpoint("OtlpHttp", endpointConfiguration =>
            {
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

                    _frontendEndPointAccessor = _otlpServiceGrpcEndPointAccessor;
                }

                endpointConfiguration.ListenOptions.UseOtlpConnection();

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
                var resolvedAddress = address.Scheme.ToLowerInvariant() + Uri.SchemeDelimiter + address.Host.ToLowerInvariant() + ":" + endpoint.Port.ToString(CultureInfo.InvariantCulture);
                return new EndpointInfo(resolvedAddress, endpoint, endpointConfiguration.IsHttps);
            };
        }
    }

    private static bool IsSameOrNull(Uri frontendUri, Uri? otlpUrl)
    {
        return otlpUrl == null || (frontendUri == otlpUrl && otlpUrl.Port != 0);
    }

    private static void ConfigureAuthentication(WebApplicationBuilder builder, DashboardOptions dashboardOptions)
    {
        var authentication = builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddScheme<OtlpCompositeAuthenticationHandlerOptions, OtlpCompositeAuthenticationHandler>(OtlpCompositeAuthenticationDefaults.AuthenticationScheme, o => { })
            .AddScheme<OtlpApiKeyAuthenticationHandlerOptions, OtlpApiKeyAuthenticationHandler>(OtlpApiKeyAuthenticationDefaults.AuthenticationScheme, o => { })
            .AddScheme<OtlpConnectionAuthenticationHandlerOptions, OtlpConnectionAuthenticationHandler>(OtlpConnectionAuthenticationDefaults.AuthenticationScheme, o => { })
            .AddCertificate(options =>
            {
                // Bind options to configuration so they can be overridden by environment variables.
                builder.Configuration.Bind("Dashboard:Otlp:CertificateAuthOptions", options);

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

        switch (dashboardOptions.Frontend.AuthMode)
        {
            case FrontendAuthMode.OpenIdConnect:
                authentication.AddPolicyScheme(FrontendAuthenticationDefaults.AuthenticationScheme, displayName: FrontendAuthenticationDefaults.AuthenticationScheme, o =>
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
                authentication.AddPolicyScheme(FrontendAuthenticationDefaults.AuthenticationScheme, displayName: FrontendAuthenticationDefaults.AuthenticationScheme, o =>
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
        }

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(
                name: OtlpAuthorization.PolicyName,
                policy: new AuthorizationPolicyBuilder(OtlpCompositeAuthenticationDefaults.AuthenticationScheme)
                    .RequireClaim(OtlpAuthorization.OtlpClaimName)
                    .Build());

            switch (dashboardOptions.Frontend.AuthMode)
            {
                case FrontendAuthMode.OpenIdConnect:
                    options.AddPolicy(
                        name: FrontendAuthorizationDefaults.PolicyName,
                        policy: new AuthorizationPolicyBuilder(FrontendAuthenticationDefaults.AuthenticationScheme)
                            .RequireOpenIdClaims(options: dashboardOptions.Frontend.OpenIdConnect)
                            .Build());
                    break;
                case FrontendAuthMode.BrowserToken:
                    options.AddPolicy(
                        name: FrontendAuthorizationDefaults.PolicyName,
                        policy: new AuthorizationPolicyBuilder(FrontendAuthenticationDefaults.AuthenticationScheme)
                            .RequireClaim(FrontendAuthorizationDefaults.BrowserTokenClaimName)
                            .Build());
                    break;
                case FrontendAuthMode.Unsecured:
                    options.AddPolicy(
                        name: FrontendAuthorizationDefaults.PolicyName,
                        policy: new AuthorizationPolicyBuilder()
                            .RequireAssertion(_ =>
                            {
                                // Frontend is unsecured so our policy doesn't require anything.
                                return true;
                            })
                            .Build());
                    break;
                default:
                    throw new NotSupportedException($"Unexpected {nameof(FrontendAuthMode)} enum member: {dashboardOptions.Frontend.AuthMode}");
            }
        });
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
        Debug.Assert(_validationFailures.Count == 0);
        return _app.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        Debug.Assert(_validationFailures.Count == 0);
        return _app.StopAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _app.DisposeAsync();
    }

    private static bool IsHttpsOrNull(Uri? uri) => uri == null || string.Equals(uri.Scheme, "https", StringComparison.Ordinal);

    public static class FrontendAuthenticationDefaults
    {
        public const string AuthenticationScheme = "Frontend";
    }
}

public record EndpointInfo(string Address, IPEndPoint EndPoint, bool isHttps);

public static class FrontendAuthorizationDefaults
{
    public const string PolicyName = "Frontend";
    public const string BrowserTokenClaimName = "BrowserTokenClaim";
}
