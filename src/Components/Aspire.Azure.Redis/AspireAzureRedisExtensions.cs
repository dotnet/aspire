// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Microsoft.Extensions.Hosting;

public static class AspireAzureRedisExtensions
{
    /// <summary>
    /// Configures Azure Redis Cache to authenticate to it using a Service Principal.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to read update from.</param>
    /// <param name="clientId">The Client ID of the Service Principal.</param>
    /// <param name="principalId">The Principal (object) ID of the Service Principal.</param>
    /// <param name="tenantId">The Tenant ID of the Service Principal.</param>
    /// <param name="secret">The Service Principal secret.</param>
    /// <param name="name">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
    /// <remarks>
    /// This needs to be invoked after the services and configuration have been added to the builder.
    /// </remarks>
    public static async Task<IHostApplicationBuilder> ConfigureAzureRedisServicePrincipal(this IHostApplicationBuilder builder, string clientId, string principalId, string tenantId, string secret, string? name = null)
    {
        var options = new ConfigurationOptions();

        options = await options.ConfigureForAzureWithServicePrincipalAsync(clientId, principalId, tenantId, secret).ConfigureAwait(false);

        builder.Services.Configure<ConfigurationOptions>(
            name ?? Options.Options.DefaultName,
            configurationOptions =>
            {
                configurationOptions.Defaults = options.Defaults;
            });

        return builder;
    }

    /// <summary>
    /// Configures Azure Redis Cache to authenticate to it using a System Assigned Identity.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to read update from.</param>
    /// <param name="principalId">The Principal (object) ID of the Service Principal.</param>
    /// <param name="name">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
    /// <remarks>
    /// This needs to be invoked after the services and configuration have been added to the builder.
    /// </remarks>
    public static async Task<IHostApplicationBuilder> ConfigureAzureRedisSystemAssignedManagedIdentity(this IHostApplicationBuilder builder, string principalId, string? name = null)
    {
        var options = new ConfigurationOptions();

        options = await options.ConfigureForAzureWithSystemAssignedManagedIdentityAsync(principalId).ConfigureAwait(false);

        builder.Services.Configure<ConfigurationOptions>(
            name ?? Options.Options.DefaultName,
            configurationOptions =>
            {
                configurationOptions.Defaults = options.Defaults;
            });

        return builder;
    }

    /// <summary>
    /// Configures Azure Redis Cache to authenticate to it using a User Assigned Identity.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to read update from.</param>
    /// <param name="clientId">The Client ID of the Service Principal.</param>
    /// <param name="principalId">The Principal (object) ID of the Service Principal.</param>
    /// <param name="name">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
    /// <remarks>
    /// This needs to be invoked after the services and configuration have been added to the builder.
    /// </remarks>
    public static async Task<IHostApplicationBuilder> ConfigureAzureRedisUserAssignedManagedIdentity(this IHostApplicationBuilder builder, string clientId, string principalId, string? name = null)
    {
        var options = new ConfigurationOptions();

        options = await options.ConfigureForAzureWithUserAssignedManagedIdentityAsync(clientId, principalId).ConfigureAwait(false);

        builder.Services.Configure<ConfigurationOptions>(
            name ?? Options.Options.DefaultName,
            configurationOptions =>
            {
                configurationOptions.Defaults = options.Defaults;
            });

        return builder;
    }
}
