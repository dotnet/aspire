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
public static class AspireKafkaConsumerExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Confluent:Kafka:Consumer";

    /// <summary>
    /// Registers <see cref="IConsumer{TKey,TValue}"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method for customizing the <see cref="KafkaConsumerSettings"/>.</param>
    /// <param name="configureBuilder">An optional method used for customizing the <see cref="ConsumerBuilder{TKey,TValue}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Kafka:Consumer" section.</remarks>
    public static void AddKafkaConsumer<TKey, TValue>(this IHostApplicationBuilder builder, string connectionName, Action<KafkaConsumerSettings>? configureSettings = null, Action<ConsumerBuilder<TKey, TValue>>? configureBuilder = null)
        => AddKafkaConsumer(builder, DefaultConfigSectionName, configureSettings, configureBuilder, connectionName, serviceKey: null);

    /// <summary>
    /// Registers <see cref="IConsumer{TKey,TValue}"/> as a keyed singleton for the given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method for customizing the <see cref="KafkaConsumerSettings"/>.</param>
    /// <param name="configureBuilder">An optional method used for customizing the <see cref="ConsumerBuilder{TKey,TValue}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Kafka:Consumer:{name}" section.</remarks>
    public static void AddKeyedKafkaConsumer<TKey, TValue>(this IHostApplicationBuilder builder, string name, Action<KafkaConsumerSettings>? configureSettings = null, Action<ConsumerBuilder<TKey, TValue>>? configureBuilder = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        AddKafkaConsumer(builder, $"{DefaultConfigSectionName}:{name}", configureSettings, configureBuilder, connectionName: name, serviceKey: name);
    }

    private static void AddKafkaConsumer<TKey, TValue>(
        IHostApplicationBuilder builder,
        string configurationSectionName,
        Action<KafkaConsumerSettings>? configureSettings,
        Action<ConsumerBuilder<TKey, TValue>>? configureBuilder,
        string connectionName,
        string? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = BuildConsumerSettings(builder, configurationSectionName, configureSettings, connectionName);

        if (serviceKey is null)
        {
            builder.Services.AddSingleton<ConsumerConnectionFactory<TKey, TValue>>(sp => CreateConsumerConnectionFactory<TKey, TValue>(sp, configureBuilder, settings));
            builder.Services.AddSingleton<IConsumer<TKey, TValue>>(sp => sp.GetRequiredService<ConsumerConnectionFactory<TKey, TValue>>().Create());
        }
        else
        {
            builder.Services.AddKeyedSingleton<ConsumerConnectionFactory<TKey, TValue>>(serviceKey, (sp, key) => CreateConsumerConnectionFactory<TKey, TValue>(sp, configureBuilder, settings));
            builder.Services.AddKeyedSingleton<IConsumer<TKey, TValue>>(serviceKey, (sp, key) => sp.GetRequiredKeyedService<ConsumerConnectionFactory<TKey, TValue>>(key).Create());
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
                ? ConfluentKafkaCommon.ConsumerHealthCheckName
                : string.Concat(ConfluentKafkaCommon.KeyedConsumerHealthCheckName, connectionName);

            builder.TryAddHealthCheck(new HealthCheckRegistration(healthCheckName,
                sp =>
                {
                    var connectionFactory = serviceKey is null
                        ? sp.GetRequiredService<ConsumerConnectionFactory<TKey, TValue>>()
                        : sp.GetRequiredKeyedService<ConsumerConnectionFactory<TKey, TValue>>(serviceKey);

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

    private static ConsumerConnectionFactory<TKey, TValue> CreateConsumerConnectionFactory<TKey, TValue>(IServiceProvider serviceProvider, Action<ConsumerBuilder<TKey, TValue>>? configureBuilder, KafkaConsumerSettings settings)
        => new(CreateConsumerBuilder(serviceProvider, configureBuilder, settings), settings.Config);

    private static ConsumerBuilder<TKey, TValue> CreateConsumerBuilder<TKey, TValue>(IServiceProvider serviceProvider, Action<ConsumerBuilder<TKey, TValue>>? configureBuilder, KafkaConsumerSettings settings)
    {
        settings.Validate();

        ConsumerBuilder<TKey, TValue> builder = new(settings.Config);
        ILogger logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(ConfluentKafkaCommon.LogCategoryName);
        configureBuilder?.Invoke(builder);

        try
        {
            void OnLog(IConsumer<TKey, TValue> _, LogMessage logMessage) =>
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
            void OnStatistics(IConsumer<TKey, TValue> _, string json)
            {
                if (string.IsNullOrEmpty(json))
                {
                    return;
                }

                // StatisticsHandler is called on the consume thread, so we need to offload the processing
                // to avoid slowing the consumer down.
                channel.Writer.TryWrite(json);
            }

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

    private static KafkaConsumerSettings BuildConsumerSettings(IHostApplicationBuilder builder, string configurationSectionName, Action<KafkaConsumerSettings>? configureSettings, string connectionName)
    {
        var configSection = builder.Configuration.GetSection(configurationSectionName);
        KafkaConsumerSettings settings = new();
        configSection.Bind(settings);

        // Manually bind the ConsumerConfig until https://github.com/dotnet/runtime/issues/96652 is fixed
        configSection.GetSection(nameof(KafkaConsumerSettings.Config)).Bind(settings.Config);

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ConnectionString = connectionString;
        }

        configureSettings?.Invoke(settings);

        settings.Consolidate();
        return settings;
    }
}
