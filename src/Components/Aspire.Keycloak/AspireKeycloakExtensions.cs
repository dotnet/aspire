// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering Keycloak-related services in an <see cref="AuthenticationBuilder"/>.
/// </summary>
public static class AspireKeycloakExtensions
{
    private const string KeycloakBackchannel = nameof(KeycloakBackchannel);

    /// <summary>
    /// Adds Keycloak JWT Bearer authentication to the application.
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder" /> to add services to.</param>
    /// <param name="serviceName">The name of the service used to resolve the Keycloak server URL.</param>
    /// <param name="realm">The realm of the Keycloak server to connect to.</param>
    /// <param name="authenticationScheme">The authentication scheme name. Default is "Bearer".</param>
    /// <param name="configureJwtBearerOptions">An optional action to configure the <see cref="JwtBearerOptions"/>.</param>
    /// <remarks>
    /// The <paramref name="serviceName"/> is used to resolve the Keycloak server URL and is combined with the realm to form the authority URL.
    /// For example, if <paramref name="serviceName"/> is "keycloak" and <paramref name="realm"/> is "myrealm", the authority URL will be "https+http://keycloak/realms/myrealm".
    /// </remarks>
    public static void AddKeycloakJwtBearer(
        this AuthenticationBuilder builder,
        string serviceName,
        string realm,
        string authenticationScheme = JwtBearerDefaults.AuthenticationScheme,
        Action<JwtBearerOptions>? configureJwtBearerOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddJwtBearer(authenticationScheme);

        builder.Services.AddHttpClient(KeycloakBackchannel);

        builder.Services
               .AddOptions<JwtBearerOptions>(authenticationScheme)
               .Configure<IConfiguration, IHttpClientFactory, IHostEnvironment>((options, configuration, httpClientFactory, hostEnvironment) =>
               {
                   options.Backchannel = httpClientFactory.CreateClient(KeycloakBackchannel);
                   options.Authority = GetAuthorityUri(serviceName, realm);

                   configureJwtBearerOptions?.Invoke(options);
               });
    }

    /// <summary>
    /// Adds Keycloak OpenID Connect authentication to the application.
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder" /> to add services to.</param>
    /// <param name="serviceName">The name of the service used to resolve the Keycloak server URL.</param>
    /// <param name="realm">The realm of the Keycloak server to connect to.</param>
    /// <param name="openIdConnectScheme">The OpenID Connect authentication scheme name. Default is "OpenIdConnect".</param>
    /// <param name="cookieScheme">The cookie authentication scheme name. Default is "Cookie".</param>
    /// <param name="configureOpenIdConnectOptions">An optional action to configure the <see cref="OpenIdConnectOptions"/>.</param>
    /// <remarks>
    /// The <paramref name="serviceName"/> is used to resolve the Keycloak server URL and is combined with the realm to form the authority URL.
    /// For example, if <paramref name="serviceName"/> is "keycloak" and <paramref name="realm"/> is "myrealm", the authority URL will be "https+http://keycloak/realms/myrealm".
    /// </remarks>
    public static void AddKeycloakOpenIdConnect(
        this AuthenticationBuilder builder,
        string serviceName,
        string realm,
        string openIdConnectScheme = OpenIdConnectDefaults.AuthenticationScheme,
        string cookieScheme = CookieAuthenticationDefaults.AuthenticationScheme,
        Action<OpenIdConnectOptions>? configureOpenIdConnectOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddCookie(cookieScheme)
               .AddOpenIdConnect(openIdConnectScheme, options => { });

        builder.Services.AddHttpClient(KeycloakBackchannel);

        builder.Services
               .AddOptions<OpenIdConnectOptions>(openIdConnectScheme)
               .Configure<IConfiguration, IHttpClientFactory, IHostEnvironment>((options, configuration, httpClientFactory, hostEnvironment) =>
               {
                   options.Backchannel = httpClientFactory.CreateClient(KeycloakBackchannel);
                   options.Authority = GetAuthorityUri(serviceName, realm);
                   options.SignInScheme = cookieScheme;
                   options.SignOutScheme = openIdConnectScheme;

                   configureOpenIdConnectOptions?.Invoke(options);
               });
    }

    private static string GetAuthorityUri(
        string connectionName,
        string realm)
    {
        return $"https+http://{connectionName}/realms/{realm}";
    }
}
