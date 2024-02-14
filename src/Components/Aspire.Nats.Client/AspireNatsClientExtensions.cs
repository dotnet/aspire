// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire;
using Aspire.Nats.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace Microsoft.Extensions.Hosting;

public static class AspireNatsClientExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Nats:Client";

    public static void AddNats(this IHostApplicationBuilder builder,
        string connectionName,
        Action<NatsClientSettings>? configureSettings = null,
        Func<NatsOpts, NatsOpts>? configureOptions = null)
        => AddNats(
            builder,
            configurationSectionName: DefaultConfigSectionName,
            connectionName: connectionName,
            serviceKey: null,
            configureSettings: configureSettings,
            configureOptions: configureOptions);

    public static void AddKeyedNats(this IHostApplicationBuilder builder,
        string name,
        Action<NatsClientSettings>? configureSettings = null,
        Func<NatsOpts, NatsOpts>? configureOptions = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        AddNats(
            builder,
            configurationSectionName: $"{DefaultConfigSectionName}:{name}",
            connectionName: name,
            serviceKey: name,
            configureSettings: configureSettings,
            configureOptions: configureOptions);
    }

    private static void AddNats(this IHostApplicationBuilder builder,
        string configurationSectionName,
        string connectionName,
        object? serviceKey,
        Action<NatsClientSettings>? configureSettings,
        Func<NatsOpts, NatsOpts>? configureOptions)
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

    public static void AddNatsJetStream(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton<INatsJSContext>(static provider =>
        {
            return new NatsJSContextFactory().CreateContext(provider.GetService<INatsConnection>()!);
        });
    }
}
