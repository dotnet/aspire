// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire;
using Aspire.Seq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for connecting a project's OpenTelemetry log events and spans to Seq.
/// </summary>
public static class AspireSeqExtensions
{
    const string ConnectionStringConfigurationKeyPrefix = "ConnectionStrings:";
    const string DefaultConnectionStringConfigurationKey = $"{ConnectionStringConfigurationKeyPrefix}seq";

    /// <summary>
    /// Registers OTLP log and trace exporters to send to Seq.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    public static void AddSeqEndpoint(this IHostApplicationBuilder builder, string name, Action<SeqSettings>? configureSettings = null)
    {
        var settings = GetSettings(builder, configureSettings);

        var seqUri = !string.IsNullOrEmpty(settings.ServerUrl)
            ? settings.ServerUrl
            : (builder.Configuration[string.IsNullOrEmpty(name)
                ? DefaultConnectionStringConfigurationKey
                : $"{ConnectionStringConfigurationKeyPrefix}{name}"]) ?? "http://localhost:5341";

        builder.Services.Configure<OpenTelemetryLoggerOptions>(logging => logging.AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri($"{seqUri}/ingest/otlp/v1/logs");
            opt.Protocol = OtlpExportProtocol.HttpProtobuf;
            if (!string.IsNullOrEmpty(settings.ApiKey))
            {
                opt.Headers = $"X-Seq-ApiKey={settings.ApiKey}";
            }
        }));
        builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing
            .AddOtlpExporter(opt =>
                {
                    opt.Endpoint = new Uri($"{seqUri}/ingest/otlp/v1/traces");
                    opt.Protocol = OtlpExportProtocol.HttpProtobuf;
                    if (!string.IsNullOrEmpty(settings.ApiKey))
                    {
                        opt.Headers = $"X-Seq-ApiKey={settings.ApiKey}";
                    }
                }
            ));

        if (settings.HealthChecks)
        {
            builder.TryAddHealthCheck(new HealthCheckRegistration(
                "Seq",
                _ => new SeqHealthCheck(seqUri),
                failureStatus: default,
                tags: default));
        }
    }

    static SeqSettings GetSettings(this IHostApplicationBuilder builder, Action<SeqSettings>? configureSettings = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = new SeqSettings();
        builder.Configuration.GetSection("Aspire:Seq").Bind(settings);

        configureSettings?.Invoke(settings);
        return settings;
    }
}
