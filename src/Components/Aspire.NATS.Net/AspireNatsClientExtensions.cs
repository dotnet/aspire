// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire;
using Aspire.NATS.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for connecting NATS server with NATS client
/// </summary>
public static class AspireNatsClientExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Nats:Client";

    /// <summary>
    /// Registers <see cref="INatsConnection"/> service for connecting NATS server with NATS client.
    /// Configures health check and logging for the NATS client.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureOptions">An optional delegate that can be used for customizing NATS options that aren't exposed as standard configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown if mandatory <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="NatsClientSettings.ConnectionString"/> is not provided.</exception>
    public static void AddNatsClient(this IHostApplicationBuilder builder, string connectionName, Action<NatsClientSettings>? configureSettings = null, Func<NatsOpts, NatsOpts>? configureOptions = null)
        => AddNatsClient(builder, configurationSectionName: DefaultConfigSectionName, connectionName: connectionName, serviceKey: null, configureSettings: configureSettings, configureOptions: configureOptions);

    /// <summary>
    /// Registers <see cref="INatsConnection"/> as a keyed service for given <paramref name="name"/> for connecting NATS server with NATS client.
    /// Configures health check and logging for the NATS client.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureOptions">An optional delegate that can be used for customizing NATS options that aren't exposed as standard configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if mandatory <paramref name="name"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="NatsClientSettings.ConnectionString"/> is not provided.</exception>
    public static void AddKeyedNatsClient(this IHostApplicationBuilder builder, string name, Action<NatsClientSettings>? configureSettings = null, Func<NatsOpts, NatsOpts>? configureOptions = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        AddNatsClient(builder, configurationSectionName: $"{DefaultConfigSectionName}:{name}", connectionName: name, serviceKey: name, configureSettings: configureSettings, configureOptions: configureOptions);
    }

    private static void AddNatsClient(this IHostApplicationBuilder builder, string configurationSectionName, string connectionName, object? serviceKey, Action<NatsClientSettings>? configureSettings, Func<NatsOpts, NatsOpts>? configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);

        NatsClientSettings settings = new();
        builder.Configuration.GetSection(configurationSectionName).Bind(settings);

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ConnectionString = connectionString;
        }

        configureSettings?.Invoke(settings);

        NatsConnection Factory(IServiceProvider provider)
        {
            var options = NatsOpts.Default with
            {
                LoggerFactory = provider.GetRequiredService<ILoggerFactory>(),
            };

            if (configureOptions != null)
            {
                options = configureOptions(options);
            }

            if (settings.ConnectionString == null)
            {
                throw new InvalidOperationException($"NATS connection string not found: {connectionName}");
            }

            options = options with { Url = settings.ConnectionString };

            return new NatsConnection(options);
        }

        if (serviceKey == null)
        {
            builder.Services.TryAddSingleton(Factory);
            builder.Services.TryAddSingleton<INatsConnection>(static provider => provider.GetRequiredService<NatsConnection>());
        }
        else
        {
            builder.Services.TryAddKeyedSingleton<NatsConnection>(serviceKey, (provider, _) => Factory(provider));
            builder.Services.TryAddKeyedSingleton<INatsConnection>(serviceKey, static (provider, key) => provider.GetRequiredKeyedService<NatsConnection>(key));
        }

        if (settings.HealthChecks)
        {
            builder.TryAddHealthCheck(new HealthCheckRegistration(
                serviceKey is null ? "NATS" : $"NATS_{connectionName}",
                sp => new NatsHealthCheck(serviceKey is null
                    ? sp.GetRequiredService<INatsConnection>()
                    : sp.GetRequiredKeyedService<INatsConnection>(serviceKey)),
                failureStatus: default,
                tags: default,
                timeout: default));
        }
    }

    /// <summary>
    /// Registers <see cref="INatsJSContext"/> service for NATS JetStream operations.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <exception cref="ArgumentNullException">Thrown if mandatory <paramref name="builder"/> is null.</exception>
    public static void AddNatsJetStream(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton<INatsJSContext>(static provider =>
        {
            return new NatsJSContextFactory().CreateContext(provider.GetService<INatsConnection>()!);
        });
    }
}
