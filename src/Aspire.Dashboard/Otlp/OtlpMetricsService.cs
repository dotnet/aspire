// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using OpenTelemetry.Proto.Collector.Metrics.V1;

namespace Aspire.Dashboard.Otlp;

public sealed class OtlpMetricsService(ILogger<OtlpMetricsService> logger, TelemetryRepository telemetryRepository)
{
    public ExportMetricsServiceResponse Export(ExportMetricsServiceRequest request)
    {
        var addContext = new AddContext();
        telemetryRepository.AddMetrics(addContext, request.ResourceMetrics);

        logger.LogDebug("Processed metrics export. Failure count: {FailureCount}", addContext.FailureCount);

        return new ExportMetricsServiceResponse
        {
            PartialSuccess = new ExportMetricsPartialSuccess
            {
                RejectedDataPoints = addContext.FailureCount
            }
        };
    }
}
