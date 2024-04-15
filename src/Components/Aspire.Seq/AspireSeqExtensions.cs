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
    public static void AddSeqEndpoint(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<SeqSettings>? configureSettings = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = new SeqSettings();
        builder.Configuration.GetSection("Aspire:Seq").Bind(settings);
        settings.ServerUrl = builder.Configuration.GetConnectionString(connectionName) ?? "http://localhost:5341";

        settings.LogExporterOptions.Endpoint = new Uri($"{settings.ServerUrl}/ingest/otlp/v1/logs");
        settings.LogExporterOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
        if (!string.IsNullOrEmpty(settings.ApiKey))
        {
            settings.LogExporterOptions.Headers = $"X-Seq-ApiKey={settings.ApiKey}";
        }
        settings.TraceExporterOptions.Endpoint = new Uri($"{settings.ServerUrl}/ingest/otlp/v1/traces");
        settings.TraceExporterOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
        if (!string.IsNullOrEmpty(settings.ApiKey))
        {
            settings.TraceExporterOptions.Headers = $"X-Seq-ApiKey={settings.ApiKey}";
        }

        configureSettings?.Invoke(settings);

        builder.Services.Configure<OpenTelemetryLoggerOptions>(logging => logging.AddProcessor(
            sp => new BatchLogRecordExportProcessor(new OtlpLogExporter(settings.LogExporterOptions))
            ));

        builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddProcessor(
            sp => new BatchActivityExportProcessor(new OtlpTraceExporter(settings.TraceExporterOptions))
            ));

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
