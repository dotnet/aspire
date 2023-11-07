// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;
using Aspire;
using Aspire.RabbitMQ.Client;
using HealthChecks.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for connecting to a RabbitMQ message broker.
/// </summary>
public static class AspireRabbitMQExtensions
{
    private const string ActivitySourceName = "Aspire.RabbitMQ.Client";
    private static readonly ActivitySource s_activitySource = new ActivitySource(ActivitySourceName);
    private const string DefaultConfigSectionName = "Aspire:RabbitMQ:Client";

    /// <summary>
    /// Registers <see cref="IConnection"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging, and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="RabbitMQClientSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureConnectionFactory">An optional method that can be used for customizing the <see cref="IConnectionFactory"/>. It's invoked after the options are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:RabbitMQ:Client" section.</remarks>
    public static void AddRabbitMQ(this IHostApplicationBuilder builder, string connectionName, Action<RabbitMQClientSettings>? configureSettings = null, Action<IConnectionFactory>? configureConnectionFactory = null)
        => AddRabbitMQ(builder, DefaultConfigSectionName, configureSettings, configureConnectionFactory, connectionName, serviceKey: null);

    /// <summary>
    /// Registers <see cref="IConnection"/> as a keyed singleton for the given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging, and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="RabbitMQClientSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureConnectionFactory">An optional method that can be used for customizing the <see cref="IConnectionFactory"/>. It's invoked after the options are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:RabbitMQ:Client:{name}" section.</remarks>
    public static void AddKeyedRabbitMQ(this IHostApplicationBuilder builder, string name, Action<RabbitMQClientSettings>? configureSettings = null, Action<IConnectionFactory>? configureConnectionFactory = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        AddRabbitMQ(builder, $"{DefaultConfigSectionName}:{name}", configureSettings, configureConnectionFactory, connectionName: name, serviceKey: name);
    }

    private static void AddRabbitMQ(
        IHostApplicationBuilder builder,
        string configurationSectionName,
        Action<RabbitMQClientSettings>? configureSettings,
        Action<IConnectionFactory>? configureConnectionFactory,
        string connectionName,
        object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var configSection = builder.Configuration.GetSection(configurationSectionName);

        var settings = new RabbitMQClientSettings();
        configSection.Bind(settings);

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ConnectionString = connectionString;
        }

        configureSettings?.Invoke(settings);

        IConnectionFactory CreateConnectionFactory(IServiceProvider sp)
        {
            // ensure the log forwarder is initialized
            sp.GetRequiredService<RabbitMQEventSourceLogForwarder>().Start();

            var factory = new ConnectionFactory();

            var configurationOptionsSection = configSection.GetSection("ConnectionFactory");
            configurationOptionsSection.Bind(factory);

            // the connection string from settings should win over the one from the ConnectionFactory section
            var connectionString = settings.ConnectionString;
            if (!string.IsNullOrEmpty(connectionString))
            {
                factory.Uri = new(connectionString);
            }

            configureConnectionFactory?.Invoke(factory);

            return factory;
        }

        if (serviceKey is null)
        {
            builder.Services.AddSingleton<IConnectionFactory>(CreateConnectionFactory);
            builder.Services.AddSingleton<IConnection>(sp => CreateConnection(sp.GetRequiredService<IConnectionFactory>(), settings.MaxConnectRetryCount));
        }
        else
        {
            builder.Services.AddKeyedSingleton<IConnectionFactory>(serviceKey, (sp, _) => CreateConnectionFactory(sp));
            builder.Services.AddKeyedSingleton<IConnection>(serviceKey, (sp, key) => CreateConnection(sp.GetRequiredKeyedService<IConnectionFactory>(key), settings.MaxConnectRetryCount));
        }

        builder.Services.AddSingleton<RabbitMQEventSourceLogForwarder>();

        if (settings.Tracing)
        {
            // Note that RabbitMQ.Client v6.6 doesn't have built-in support for tracing. See https://github.com/rabbitmq/rabbitmq-dotnet-client/pull/1261

            builder.Services.AddOpenTelemetry()
                .WithTracing(traceBuilder => traceBuilder.AddSource(ActivitySourceName));
        }

        if (settings.HealthChecks)
        {
            builder.TryAddHealthCheck(new HealthCheckRegistration(
                serviceKey is null ? "RabbitMQ.Client" : $"RabbitMQ.Client_{connectionName}",
                sp =>
                {
                    try
                    {
                        // if the IConnection can't be resolved, make a health check that will fail
                        var options = new RabbitMQHealthCheckOptions();
                        options.Connection = serviceKey is null ? sp.GetRequiredService<IConnection>() : sp.GetRequiredKeyedService<IConnection>(serviceKey);
                        return new RabbitMQHealthCheck(options);
                    }
                    catch (Exception ex)
                    {
                        return new FailedHealthCheck(ex);
                    }
                },
                failureStatus: default,
                tags: default));
        }
    }

    private sealed class FailedHealthCheck(Exception ex) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new HealthCheckResult(context.Registration.FailureStatus, exception: ex));
        }
    }

    private static IConnection CreateConnection(IConnectionFactory factory, int retryCount)
    {
        var policy = Policy
            .Handle<SocketException>().Or<BrokerUnreachableException>()
            .WaitAndRetry(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        using var activity = s_activitySource.StartActivity("rabbitmq connect", ActivityKind.Client);
        AddRabbitMQTags(activity);

        return policy.Execute(() =>
        {
            using var connectAttemptActivity = s_activitySource.StartActivity("rabbitmq connect attempt", ActivityKind.Client);
            AddRabbitMQTags(connectAttemptActivity, "connect");

            try
            {
                return factory.CreateConnection();
            }
            catch (Exception ex)
            {
                if (connectAttemptActivity is not null)
                {
                    connectAttemptActivity.AddTag("exception.message", ex.Message);
                    connectAttemptActivity.AddTag("exception.stacktrace", ex.ToString());
                    connectAttemptActivity.AddTag("exception.type", ex.GetType().FullName);
                    connectAttemptActivity.SetStatus(ActivityStatusCode.Error);
                }
                throw;
            }
        });
    }

    private static void AddRabbitMQTags(Activity? activity, string? operation = null)
    {
        if (activity is null)
        {
            return;
        }

        activity.AddTag("messaging.system", "rabbitmq");
        if (operation is not null)
        {
            activity.AddTag("messaging.operation", operation);
        }
    }
}
