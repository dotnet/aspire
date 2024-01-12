// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire;
using Aspire.Confluent.Kafka;
using Confluent.Kafka;
using HealthChecks.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for connecting to a Kafka broker.
/// </summary>
public static class AspireKafkaProducerExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Confluent:Kafka:Producer";

    /// <summary>
    /// Registers <see cref="IProducer{TKey,TValue}"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method used for customizing the <see cref="KafkaProducerSettings"/>.</param>
    /// <param name="configureBuilder">A method used for customizing the <see cref="ProducerBuilder{TKey,TValue}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Kafka:Producer" section.</remarks>
    public static void AddKafkaProducer<TKey, TValue>(this IHostApplicationBuilder builder, string connectionName, Action<KafkaProducerSettings>? configureSettings = null, Action<ProducerBuilder<TKey, TValue>>? configureBuilder = null)
        => AddKafkaProducer(builder, DefaultConfigSectionName, configureSettings, configureBuilder, connectionName, serviceKey: null);

    /// <summary>
    /// Registers <see cref="IProducer{TKey,TValue}"/> as a keyed singleton for the given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method used for customizing the <see cref="KafkaProducerSettings"/>.</param>
    /// <param name="configureBuilder">An optional method used for customizing the <see cref="ProducerBuilder{TKey,TValue}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Kafka:Producer:{name}" section.</remarks>
    public static void AddKeyedKafkaProducer<TKey, TValue>(this IHostApplicationBuilder builder, string name, Action<KafkaProducerSettings>? configureSettings = null, Action<ProducerBuilder<TKey, TValue>>? configureBuilder = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        AddKafkaProducer(builder, $"{DefaultConfigSectionName}:{name}", configureSettings, configureBuilder, connectionName: name, serviceKey: name);
    }

    private static void AddKafkaProducer<TKey, TValue>(
        IHostApplicationBuilder builder,
        string configurationSectionName,
        Action<KafkaProducerSettings>? configureSettings,
        Action<ProducerBuilder<TKey, TValue>>? configureBuilder,
        string connectionName,
        string? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = BuildProducerSettings(builder, configurationSectionName, configureSettings, connectionName);

        if (serviceKey is null)
        {
            builder.Services.AddSingleton<ProducerConnectionFactory<TKey, TValue>>(sp => CreateProducerConnectionFactory<TKey, TValue>(sp, configureBuilder, settings));
            builder.Services.AddSingleton<IProducer<TKey, TValue>>(sp => sp.GetRequiredService<ProducerConnectionFactory<TKey, TValue>>().Create());
        }
        else
        {
            builder.Services.AddKeyedSingleton<ProducerConnectionFactory<TKey, TValue>>(serviceKey, (sp, key) => CreateProducerConnectionFactory<TKey, TValue>(sp, configureBuilder, settings));
            builder.Services.AddKeyedSingleton<IProducer<TKey, TValue>>(serviceKey, (sp, key) => sp.GetRequiredKeyedService<ProducerConnectionFactory<TKey, TValue>>(key).Create());
        }

        if (settings.Metrics)
        {
            builder.Services.TryAddSingleton<MetricsChannel>();
            builder.Services.AddHostedService<MetricsService>();
            builder.Services.TryAddSingleton<ConfluentKafkaMetrics>();
            builder.Services.AddOpenTelemetry().WithMetrics(metricBuilderProvider => metricBuilderProvider.AddMeter(ConfluentKafkaCommon.MeterName));
        }

        if (settings.HealthChecks)
        {
            string healthCheckName = serviceKey is null
                ? ConfluentKafkaCommon.ProducerHealthCheckName
                : string.Concat(ConfluentKafkaCommon.KeyedProducerHealthCheckName, connectionName);

            builder.TryAddHealthCheck(new HealthCheckRegistration(healthCheckName,
                sp =>
                {
                    var connectionFactory = serviceKey is null
                        ? sp.GetRequiredService<ProducerConnectionFactory<TKey, TValue>>()
                        : sp.GetRequiredKeyedService<ProducerConnectionFactory<TKey, TValue>>(serviceKey);

                    var options = new KafkaHealthCheckOptions();
                    options.Configuration = new ProducerConfig(connectionFactory.Config.ToDictionary());
                    options.Configuration.SocketTimeoutMs = 1000;
                    options.Configuration.MessageTimeoutMs = 1000;
                    options.Configuration.StatisticsIntervalMs = 0;
                    return new KafkaHealthCheck(options);
                },
                failureStatus: default,
                tags: default));
        }
    }

    private static ProducerConnectionFactory<TKey, TValue> CreateProducerConnectionFactory<TKey, TValue>(IServiceProvider serviceProvider, Action<ProducerBuilder<TKey, TValue>>? configureBuilder, KafkaProducerSettings settings)
        => new(CreateProducerBuilder(serviceProvider, configureBuilder, settings), settings.Config);

    private static ProducerBuilder<TKey, TValue> CreateProducerBuilder<TKey, TValue>(IServiceProvider serviceProvider, Action<ProducerBuilder<TKey, TValue>>? configureBuilder, KafkaProducerSettings settings)
    {
        settings.Validate();

        ProducerBuilder<TKey, TValue> builder = new(settings.Config);
        ILogger logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(ConfluentKafkaCommon.LogCategoryName);
        configureBuilder?.Invoke(builder);

        try
        {
            void OnLog(IProducer<TKey, TValue> _, LogMessage logMessage) =>
               logger.Log((LogLevel)logMessage.LevelAs(LogLevelType.MicrosoftExtensionsLogging), logMessage.Facility?.GetHashCode() ?? 0, logMessage.Message, null, static (value, ex) => value);

            builder.SetLogHandler(OnLog);
        }
        catch (InvalidOperationException)
        {
            logger.LogWarning("LogHandler is already set. Skipping... No logs will be written.");
        }

        if (settings.Metrics)
        {
            MetricsChannel channel = serviceProvider.GetRequiredService<MetricsChannel>();
            void OnStatistics(IProducer<TKey, TValue> _, string json)
            {
                if (string.IsNullOrEmpty(json))
                {
                    return;
                }

                // StatisticsHandler is called on the producer poll thread, we need to offload the processing
                // to avoid slowing the producer down.
                channel.Writer.TryWrite(json);
            };

            try
            {
                builder.SetStatisticsHandler(OnStatistics);
            }
            catch (InvalidOperationException)
            {
                logger.LogWarning("StatisticsHandler is already set. Skipping... No metrics will be exposed.");
            }
        }
        return builder;
    }

    private static KafkaProducerSettings BuildProducerSettings(IHostApplicationBuilder builder, string configurationSectionName, Action<KafkaProducerSettings>? configureSettings, string connectionName)
    {
        var configSection = builder.Configuration.GetSection(configurationSectionName);
        KafkaProducerSettings settings = new();
        configSection.Bind(settings);

        // Manually bind the ProducerConfig until https://github.com/dotnet/runtime/issues/96652 is fixed
        configSection.GetSection(nameof(KafkaProducerSettings.Config)).Bind(settings.Config);

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ConnectionString = connectionString;
        }

        configureSettings?.Invoke(settings);

        settings.Consolidate();
        return settings;
    }
}
