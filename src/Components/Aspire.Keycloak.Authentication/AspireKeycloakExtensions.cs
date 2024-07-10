// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

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
    /// <remarks>
    /// The <paramref name="serviceName"/> is used to resolve the Keycloak server URL and is combined with the realm to form the authority URL.
    /// For example, if <paramref name="serviceName"/> is "keycloak" and <paramref name="realm"/> is "myrealm", the authority URL will be "https+http://keycloak/realms/myrealm".
    /// </remarks>
    public static AuthenticationBuilder AddKeycloakJwtBearer(this AuthenticationBuilder builder, string serviceName, string realm)
        => builder.AddKeycloakJwtBearer(serviceName, realm, JwtBearerDefaults.AuthenticationScheme, _ => { });

    /// <summary>
    /// Adds Keycloak JWT Bearer authentication to the application.
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder" /> to add services to.</param>
    /// <param name="serviceName">The name of the service used to resolve the Keycloak server URL.</param>
    /// <param name="realm">The realm of the Keycloak server to connect to.</param>
    /// <param name="authenticationScheme">The authentication scheme name. Default is "Bearer".</param>
    /// <remarks>
    /// The <paramref name="serviceName"/> is used to resolve the Keycloak server URL and is combined with the realm to form the authority URL.
    /// For example, if <paramref name="serviceName"/> is "keycloak" and <paramref name="realm"/> is "myrealm", the authority URL will be "https+http://keycloak/realms/myrealm".
    /// </remarks>
    public static AuthenticationBuilder AddKeycloakJwtBearer(this AuthenticationBuilder builder, string serviceName, string realm, string authenticationScheme)
        => builder.AddKeycloakJwtBearer(serviceName, realm, authenticationScheme, _ => { });

    /// <summary>
    /// Adds Keycloak JWT Bearer authentication to the application.
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder" /> to add services to.</param>
    /// <param name="serviceName">The name of the service used to resolve the Keycloak server URL.</param>
    /// <param name="realm">The realm of the Keycloak server to connect to.</param>
    /// <param name="configureOptions">An optional action to configure the <see cref="JwtBearerOptions"/>.</param>
    /// <remarks>
    /// The <paramref name="serviceName"/> is used to resolve the Keycloak server URL and is combined with the realm to form the authority URL.
    /// For example, if <paramref name="serviceName"/> is "keycloak" and <paramref name="realm"/> is "myrealm", the authority URL will be "https+http://keycloak/realms/myrealm".
    /// </remarks>
    public static AuthenticationBuilder AddKeycloakJwtBearer(this AuthenticationBuilder builder, string serviceName, string realm, Action<JwtBearerOptions> configureOptions)
        => builder.AddKeycloakJwtBearer(serviceName, realm, JwtBearerDefaults.AuthenticationScheme, configureOptions);

    /// <summary>
    /// Adds Keycloak JWT Bearer authentication to the application.
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder" /> to add services to.</param>
    /// <param name="serviceName">The name of the service used to resolve the Keycloak server URL.</param>
    /// <param name="realm">The realm of the Keycloak server to connect to.</param>
    /// <param name="authenticationScheme">The authentication scheme name. Default is "Bearer".</param>
    /// <param name="configureOptions">An action to configure the <see cref="JwtBearerOptions"/>.</param>
    /// <remarks>
    /// The <paramref name="serviceName"/> is used to resolve the Keycloak server URL and is combined with the realm to form the authority URL.
    /// For example, if <paramref name="serviceName"/> is "keycloak" and <paramref name="realm"/> is "myrealm", the authority URL will be "https+http://keycloak/realms/myrealm".
    /// </remarks>
    public static AuthenticationBuilder AddKeycloakJwtBearer(
        this AuthenticationBuilder builder,
        string serviceName,
        string realm,
        string authenticationScheme,
        Action<JwtBearerOptions> configureOptions)
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

                   configureOptions?.Invoke(options);
               });

        return builder;
    }

    /// <summary>
    /// Adds Keycloak OpenID Connect authentication to the application.
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder" /> to add services to.</param>
    /// <param name="serviceName">The name of the service used to resolve the Keycloak server URL.</param>
    /// <param name="realm">The realm of the Keycloak server to connect to.</param>
    /// <remarks>
    /// The <paramref name="serviceName"/> is used to resolve the Keycloak server URL and is combined with the realm to form the authority URL.
    /// For example, if <paramref name="serviceName"/> is "keycloak" and <paramref name="realm"/> is "myrealm", the authority URL will be "https+http://keycloak/realms/myrealm".
    /// </remarks>
    public static AuthenticationBuilder AddKeycloakOpenIdConnect(this AuthenticationBuilder builder, string serviceName, string realm)
        => builder.AddKeycloakOpenIdConnect(serviceName, realm, OpenIdConnectDefaults.AuthenticationScheme, _ => { });

    /// <summary>
    /// Adds Keycloak OpenID Connect authentication to the application.
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder" /> to add services to.</param>
    /// <param name="serviceName">The name of the service used to resolve the Keycloak server URL.</param>
    /// <param name="realm">The realm of the Keycloak server to connect to.</param>
    /// <param name="authenticationScheme">The OpenID Connect authentication scheme name. Default is "OpenIdConnect".</param>
    /// <remarks>
    /// The <paramref name="serviceName"/> is used to resolve the Keycloak server URL and is combined with the realm to form the authority URL.
    /// For example, if <paramref name="serviceName"/> is "keycloak" and <paramref name="realm"/> is "myrealm", the authority URL will be "https+http://keycloak/realms/myrealm".
    /// </remarks>
    public static AuthenticationBuilder AddKeycloakOpenIdConnect(this AuthenticationBuilder builder, string serviceName, string realm, string authenticationScheme)
        => builder.AddKeycloakOpenIdConnect(serviceName, realm, authenticationScheme, _ => { });

    /// <summary>
    /// Adds Keycloak OpenID Connect authentication to the application.
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder" /> to add services to.</param>
    /// <param name="serviceName">The name of the service used to resolve the Keycloak server URL.</param>
    /// <param name="realm">The realm of the Keycloak server to connect to.</param>
    /// <param name="configureOptions">An action to configure the <see cref="OpenIdConnectOptions"/>.</param>
    /// <remarks>
    /// The <paramref name="serviceName"/> is used to resolve the Keycloak server URL and is combined with the realm to form the authority URL.
    /// For example, if <paramref name="serviceName"/> is "keycloak" and <paramref name="realm"/> is "myrealm", the authority URL will be "https+http://keycloak/realms/myrealm".
    /// </remarks>
    public static AuthenticationBuilder AddKeycloakOpenIdConnect(this AuthenticationBuilder builder, string serviceName, string realm, Action<OpenIdConnectOptions> configureOptions)
        => builder.AddKeycloakOpenIdConnect(serviceName, realm, OpenIdConnectDefaults.AuthenticationScheme, configureOptions);

    /// <summary>
    /// Adds Keycloak OpenID Connect authentication to the application.
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder" /> to add services to.</param>
    /// <param name="serviceName">The name of the service used to resolve the Keycloak server URL.</param>
    /// <param name="realm">The realm of the Keycloak server to connect to.</param>
    /// <param name="authenticationScheme">The OpenID Connect authentication scheme name. Default is "OpenIdConnect".</param>
    /// <param name="configureOptions">An action to configure the <see cref="OpenIdConnectOptions"/>.</param>
    /// <remarks>
    /// The <paramref name="serviceName"/> is used to resolve the Keycloak server URL and is combined with the realm to form the authority URL.
    /// For example, if <paramref name="serviceName"/> is "keycloak" and <paramref name="realm"/> is "myrealm", the authority URL will be "https+http://keycloak/realms/myrealm".
    /// </remarks>
    public static AuthenticationBuilder AddKeycloakOpenIdConnect(
        this AuthenticationBuilder builder,
        string serviceName,
        string realm,
        string authenticationScheme,
        Action<OpenIdConnectOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddOpenIdConnect(authenticationScheme, options => { });

        builder.Services.AddHttpClient(KeycloakBackchannel);

        builder.Services
               .AddOptions<OpenIdConnectOptions>(authenticationScheme)
               .Configure<IConfiguration, IHttpClientFactory, IHostEnvironment>((options, configuration, httpClientFactory, hostEnvironment) =>
               {
                   options.Backchannel = httpClientFactory.CreateClient(KeycloakBackchannel);
                   options.Authority = GetAuthorityUri(serviceName, realm);

                   configureOptions?.Invoke(options);
               });

        return builder;
    }

    private static string GetAuthorityUri(
        string serviceName,
        string realm)
    {
        return $"https+http://{serviceName}/realms/{realm}";
    }
}
