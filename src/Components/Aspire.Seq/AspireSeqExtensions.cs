// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire;
using Aspire.Seq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for connecting a project's OpenTelemetry log events and spans to Seq.
/// </summary>
public static class AspireSeqExtensions
{
    /// <summary>
    /// Registers OTLP log and trace exporters to send to Seq.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="logExporterOptions">An optional delegate that can be used for customizing log exporter options.</param>
    /// <param name="traceExporterOptions">An optional delegate that can be used for customizing trace exporter options.</param>
    public static void AddSeqEndpoint(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<SeqSettings>? configureSettings = null,
        Action<OtlpExporterOptions>? logExporterOptions = null,
        Action<OtlpExporterOptions>? traceExporterOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = new SeqSettings();
        builder.Configuration.GetSection("Aspire:Seq").Bind(settings);

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ServerUrl = connectionString;
        }

        configureSettings?.Invoke(settings);

        if (string.IsNullOrEmpty(settings.ServerUrl))
        {
            settings.ServerUrl = "http://localhost:5341";
        }

        builder.Services.Configure<OpenTelemetryLoggerOptions>(logging => logging.AddProcessor(
            sp =>
            {
                var options = new OtlpExporterOptions()
                {
                    Endpoint = new Uri($"{settings.ServerUrl}/ingest/otlp/v1/logs"),
                    Protocol = OtlpExportProtocol.HttpProtobuf
                };

                if (!string.IsNullOrEmpty(settings.ApiKey))
                {
                    options.Headers = $"X-Seq-ApiKey={settings.ApiKey}";
                }

                logExporterOptions?.Invoke(options);

                return new BatchLogRecordExportProcessor(new OtlpLogExporter(options));
            }));

        builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddProcessor(
            sp =>
            {
                var options = new OtlpExporterOptions()
                {
                    Endpoint = new Uri($"{settings.ServerUrl}/ingest/otlp/v1/traces"),
                    Protocol = OtlpExportProtocol.HttpProtobuf
                };

                if (!string.IsNullOrEmpty(settings.ApiKey))
                {
                    options.Headers = $"X-Seq-ApiKey={settings.ApiKey}";
                }

                traceExporterOptions?.Invoke(options);

                return new BatchActivityExportProcessor(new OtlpTraceExporter(options));
            }));

        if (settings.HealthChecks)
        {
            builder.TryAddHealthCheck(new HealthCheckRegistration(
                "Seq",
                _ => new SeqHealthCheck(settings.ServerUrl),
                failureStatus: default,
                tags: default));
        }
    }
}
