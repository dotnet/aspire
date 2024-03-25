// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Authentication;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Proto.Collector.Metrics.V1;

namespace Aspire.Dashboard.Otlp.Grpc;

[Authorize(Policy = OtlpAuthorization.PolicyName)]
[SkipStatusCodePages]
public class OtlpMetricsService : MetricsService.MetricsServiceBase
{
    private readonly ILogger<OtlpMetricsService> _logger;
    private readonly TelemetryRepository _telemetryRepository;

    public OtlpMetricsService(ILogger<OtlpMetricsService> logger, TelemetryRepository telemetryRepository)
    {
        _logger = logger;
        _telemetryRepository = telemetryRepository;
    }

    public override Task<ExportMetricsServiceResponse> Export(ExportMetricsServiceRequest request, ServerCallContext context)
    {
        var addContext = new AddContext();
        _telemetryRepository.AddMetrics(addContext, request.ResourceMetrics);

        return Task.FromResult(new ExportMetricsServiceResponse
        {
            PartialSuccess = new ExportMetricsPartialSuccess
            {
                RejectedDataPoints = addContext.FailureCount
            }
        });
    }
}
