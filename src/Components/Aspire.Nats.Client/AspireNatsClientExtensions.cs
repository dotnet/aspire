// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Nats.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;
using NATS.Client.Hosting;
using NATS.Client.JetStream;

namespace Microsoft.Extensions.Hosting;

public static class AspireNatsClientExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Nats:Client";

    public static void AddNats(this IHostApplicationBuilder builder, string connectionName, Action<NatsClientSettings>? configureSettings = null, Func<NatsOpts, NatsOpts>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        NatsClientSettings settings = new();
        builder.Configuration.GetSection(DefaultConfigSectionName).Bind(settings);

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ConnectionString = connectionString;
        }

        configureSettings?.Invoke(settings);

        if (settings.ConnectionString == null)
        {
            throw new InvalidOperationException($"NATS connection string not found: {connectionName}");
        }

        builder.Services.AddNats(configureOpts: opts =>
        {
            if (configureOptions != null)
            {
                opts = configureOptions(opts);
            }

            return opts with { Url = settings.ConnectionString };
        });
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
