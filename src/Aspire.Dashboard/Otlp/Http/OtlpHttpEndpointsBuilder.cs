// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net.Http.Headers;
using Aspire.Dashboard.Authentication;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;

namespace Aspire.Dashboard.Otlp.Http;

public static class OtlpHttpEndpointsBuilder
{
    // https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/large-object-heap
    private const int MaxSizeLessThanLOH = 84999;
    public const string ProtobufContentType = "application/x-protobuf";
    public const string JsonContentType = "application/json";

    public static void ConfigureHttpOtlp(this IEndpointRouteBuilder endpoints, Uri httpEndpoint)
    {
        var logsService = endpoints.ServiceProvider.GetRequiredService<OtlpLogsService>();
        var traceService = endpoints.ServiceProvider.GetRequiredService<OtlpTraceService>();
        var metricsService = endpoints.ServiceProvider.GetRequiredService<OtlpMetricsService>();

        endpoints.MapPost("/v1/logs", LogsEndpoint).AddOtlpHttpMetadata();
        endpoints.MapPost("/v1/traces", TracesEndpoint).AddOtlpHttpMetadata();
        endpoints.MapPost("/v1/metrics", MetricsEndpoint).AddOtlpHttpMetadata();

        async Task LogsEndpoint(HttpContext context)
        {
            switch (GetKnownContentType(context.Request.ContentType, out var charSet))
            {
                case KnownContentType.Protobuf:
                    var request = await ReadOtlpData(context, ExportLogsServiceRequest.Parser.ParseFrom).ConfigureAwait(false);
                    var response = logsService.Export(request);

                    await WriteOtlpResponse(context, response).ConfigureAwait(false);
                    break;
                case KnownContentType.Json:
                default:
                    context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                    break;
            }
        }
        async Task TracesEndpoint(HttpContext context)
        {
            switch (GetKnownContentType(context.Request.ContentType, out var charSet))
            {
                case KnownContentType.Protobuf:
                    var request = await ReadOtlpData(context, ExportTraceServiceRequest.Parser.ParseFrom).ConfigureAwait(false);
                    var response = traceService.Export(request);

                    await WriteOtlpResponse(context, response).ConfigureAwait(false);
                    break;
                case KnownContentType.Json:
                default:
                    context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                    break;
            }
        }
        async Task MetricsEndpoint(HttpContext context)
        {
            switch (GetKnownContentType(context.Request.ContentType, out var charSet))
            {
                case KnownContentType.Protobuf:
                    var request = await ReadOtlpData(context, ExportMetricsServiceRequest.Parser.ParseFrom).ConfigureAwait(false);
                    var response = metricsService.Export(request);

                    await WriteOtlpResponse(context, response).ConfigureAwait(false);
                    break;
                case KnownContentType.Json:
                default:
                    context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                    break;
            }
        }
    }

    private static async Task WriteOtlpResponse<T>(HttpContext context, T response) where T : IMessage
    {
        var ms = new MemoryStream();
        response.WriteTo(ms);
        ms.Seek(0, SeekOrigin.Begin);

        context.Response.ContentType = ProtobufContentType;
        await ms.CopyToAsync(context.Response.Body).ConfigureAwait(false);
    }

    private enum KnownContentType
    {
        None,
        Protobuf,
        Json
    }

    private static KnownContentType GetKnownContentType(string? contentType, out StringSegment charSet)
    {
        if (contentType != null && MediaTypeHeaderValue.TryParse(contentType, out var mt))
        {
            if (string.Equals(mt.MediaType, JsonContentType, StringComparison.OrdinalIgnoreCase))
            {
                charSet = mt.CharSet;
                return KnownContentType.Json;
            }

            if (string.Equals(mt.MediaType, ProtobufContentType, StringComparison.OrdinalIgnoreCase))
            {
                charSet = mt.CharSet;
                return KnownContentType.Protobuf;
            }
        }

        charSet = default;
        return KnownContentType.None;
    }

    private static IEndpointConventionBuilder AddOtlpHttpMetadata(this IEndpointConventionBuilder builder)
    {
        builder
            .RequireAuthorization(OtlpAuthorization.PolicyName)
            .Add(b => b.Metadata.Add(new SkipStatusCodePagesAttribute()));
        return builder;
    }

    private static async Task<T> ReadOtlpData<T>(
        HttpContext httpContext,
        Func<ReadOnlySequence<byte>, T> exporter)
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

            return exporter(result.Buffer);
        }
        finally
        {
            httpContext.Request.BodyReader.AdvanceTo(position);
        }
    }
}
