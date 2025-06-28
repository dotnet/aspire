using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;

namespace MetricsApp.AppHost.OpenTelemetryCollector;

internal sealed class OpenTelemetryCollectorLifecycleHook : IDistributedApplicationLifecycleHook
{
    private const string OtelExporterOtlpEndpoint = "OTEL_EXPORTER_OTLP_ENDPOINT";
    private const string OtelExporterOtlpHeaders = "OTEL_EXPORTER_OTLP_HEADERS";
    private const string OtelExporterOtlpProtocol = "http/protobuf";

    private readonly ILogger<OpenTelemetryCollectorLifecycleHook> _logger;

    public OpenTelemetryCollectorLifecycleHook(ILogger<OpenTelemetryCollectorLifecycleHook> logger)
    {
        _logger = logger;
    }

    public Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        foreach (var resource in appModel.Resources)
        {
            resource.Annotations.Add(new EnvironmentCallbackAnnotation((EnvironmentCallbackContext context) =>
            {
                if (context.EnvironmentVariables.ContainsKey(OtelExporterOtlpEndpoint))
                {
                    _logger.LogDebug("Forwarding telemetry for {ResourceName} to the collector.", resource.Name);

                    context.EnvironmentVariables[OtelExporterOtlpEndpoint] = "https://api.honeycomb.io";
                    context.EnvironmentVariables[OtelExporterOtlpHeaders] = "x-honeycomb-team=WfjkwPpUYE8GxJJFqWWe1C";
                    context.EnvironmentVariables[OtelExporterOtlpProtocol] = "http/protobuf";
                }
            }));
        }

        return Task.CompletedTask;
    }
}
