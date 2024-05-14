// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Aspire.Dashboard.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;

namespace Aspire.Dashboard.Otlp;

public static class WebApplicationExtensions
{
    // https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/large-object-heap
    private const int MaxSizeLessThanLOH = 84999;

    public static WebApplication ConfigureHttpOtlp(this WebApplication app, Uri httpEndpoint)
    {
        app.MapPost("/v1/metrics",
                async ([FromServices] OtlpMetricsService service, HttpContext httpContext) =>
                {
                    return await ExportOtlpData(httpContext,
                            sequence => service.Export(ExportMetricsServiceRequest.Parser.ParseFrom(sequence)))
                        .ConfigureAwait(false);
                }
            ).RequireAuthorization(OtlpAuthorization.PolicyName)
            .RequireHost($"*:{httpEndpoint.Port}");

        app.MapPost("/v1/traces",
                async ([FromServices] OtlpTraceService service, HttpContext httpContext) =>
                {
                    return await ExportOtlpData(httpContext,
                            sequence => service.Export(ExportTraceServiceRequest.Parser.ParseFrom(sequence)))
                        .ConfigureAwait(false);
                }
            ).RequireAuthorization(OtlpAuthorization.PolicyName)
            .RequireHost($"*:{httpEndpoint.Port}");

        app.MapPost("/v1/logs",
                async ([FromServices] OtlpLogsService service, HttpContext httpContext) =>
                {
                    return await ExportOtlpData(httpContext,
                            sequence => service.Export(ExportLogsServiceRequest.Parser.ParseFrom(sequence)))
                        .ConfigureAwait(false);
                }
            ).RequireAuthorization(OtlpAuthorization.PolicyName)
            .RequireHost($"*:{httpEndpoint.Port}");

        return app;
    }

    private static async Task<T> ExportOtlpData<T>(
        HttpContext httpContext,
        Func<ReadOnlySequence<byte>, Task<T>> exporter)
    {
        var readSize = (int?)httpContext.Request.Headers.ContentLength ?? MaxSizeLessThanLOH;
        SequencePosition position = default;
        try
        {
            var result = await httpContext.Request.BodyReader.ReadAtLeastAsync(readSize, httpContext.RequestAborted)
                .ConfigureAwait(false);
            position = result.Buffer.End;
            if (result.IsCanceled)
            {
                throw new OperationCanceledException("Read call was canceled.");
            }

            if (!result.IsCompleted || result.Buffer.Length > readSize)
            {
                // Too big!
                throw new BadHttpRequestException(
                    $"The request body was larger than the max allowed of {readSize} bytes.",
                    StatusCodes.Status400BadRequest);
            }

            return await exporter(result.Buffer).ConfigureAwait(false);
        }
        finally
        {
            httpContext.Request.BodyReader.AdvanceTo(position);
        }
    }
}
