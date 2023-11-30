// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using OpenTelemetry.Trace;
using StackExchange.Redis;

namespace Aspire.StackExchange.Redis;

// This is a temporary solution to add instrumentation for keyed service.
// This class needs to get removed once this issue got solved and published.
// https://github.com/open-telemetry/opentelemetry-dotnet-contrib/issues/1451

// This code has been copied from here
//https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/c3a4f335f5a1f4fc784d44cc99b5bf44e872a66c/src/OpenTelemetry.Instrumentation.StackExchangeRedis/TracerProviderBuilderExtensions.cs#L109
internal static class TempOpenTelemetry
{
    public static TracerProviderBuilder AddRedisInstrumentationWithKeyedService(
        this TracerProviderBuilder builder,
        object serviceKey)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureServices(services =>
        {
            services.TryAddSingleton(sp => Create(sp));
        });

        return builder
            .AddSource("OpenTelemetry.Instrumentation.StackExchangeRedis")
            .AddInstrumentation(sp =>
            {
                var instrumentation = sp.GetRequiredService<StackExchangeRedisInstrumentation>();

                var connection = (sp as IKeyedServiceProvider)!.GetKeyedService<IConnectionMultiplexer>(serviceKey);

                if (connection != null)
                {
                    instrumentation.AddConnection(Options.DefaultName, connection);
                }

                return instrumentation;
            });
    }

    private static StackExchangeRedisInstrumentation Create(IServiceProvider sp)
    {
        // StackExchangeRedisInstrumentation has an internal constructor
        var instrumentation = Activator.CreateInstance(
            typeof(StackExchangeRedisInstrumentation),
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new object[] { sp.GetRequiredService<IOptionsMonitor<StackExchangeRedisInstrumentationOptions>>() },
            null) as StackExchangeRedisInstrumentation;

        return instrumentation!;
    }
}
