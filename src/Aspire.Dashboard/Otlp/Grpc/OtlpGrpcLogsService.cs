// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Authentication;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Proto.Collector.Logs.V1;

namespace Aspire.Dashboard.Otlp.Grpc;

[Authorize(Policy = OtlpAuthorization.PolicyName)]
[SkipStatusCodePages]
public class OtlpGrpcLogsService : LogsService.LogsServiceBase
{
    private readonly ILogger<OtlpGrpcLogsService> _logger;
    private readonly OtlpLogsService _logsService;

    public OtlpGrpcLogsService(ILogger<OtlpGrpcLogsService> logger, OtlpLogsService logsService)
    {
        _logger = logger;
        _logsService = logsService;
    }

    public override Task<ExportLogsServiceResponse> Export(ExportLogsServiceRequest request, ServerCallContext context)
    {
        return _logsService.Export(request);
    }
}
