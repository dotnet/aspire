// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
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
    public const string ProtobufContentType = "application/x-protobuf";
    public const string JsonContentType = "application/json";

    public static void ConfigureHttpOtlp(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/v1")
            .AddOtlpHttpMetadata()
            .AddEndpointFilter<ErrorHandlerEndpointFilter>();

        group.MapPost("logs", LogsEndpoint);
        group.MapPost("traces", TracesEndpoint);
        group.MapPost("metrics", MetricsEndpoint);

        async Task<IResult> LogsEndpoint(HttpContext context, OtlpLogsService service)
        {
            switch (GetKnownContentType(context.Request.ContentType, out var charSet))
            {
                case KnownContentType.Protobuf:
                    var request = await ReadOtlpData(context, ExportLogsServiceRequest.Parser.ParseFrom).ConfigureAwait(false);
                    var response = service.Export(request);

                    return new ProtobufResult<ExportLogsServiceResponse>(response);
                case KnownContentType.Json:
                default:
                    return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
            }
        }
        async Task<IResult> TracesEndpoint(HttpContext context, OtlpTraceService service)
        {
            switch (GetKnownContentType(context.Request.ContentType, out var charSet))
            {
                case KnownContentType.Protobuf:
                    var request = await ReadOtlpData(context, ExportTraceServiceRequest.Parser.ParseFrom).ConfigureAwait(false);
                    var response = service.Export(request);

                    return new ProtobufResult<ExportTraceServiceResponse>(response);
                case KnownContentType.Json:
                default:
                    return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
            }
        }
        async Task<IResult> MetricsEndpoint(HttpContext context, OtlpMetricsService service)
        {
            switch (GetKnownContentType(context.Request.ContentType, out var charSet))
            {
                case KnownContentType.Protobuf:
                    var request = await ReadOtlpData(context, ExportMetricsServiceRequest.Parser.ParseFrom).ConfigureAwait(false);
                    var response = service.Export(request);

                    return new ProtobufResult<ExportMetricsServiceResponse>(response);
                case KnownContentType.Json:
                default:
                    return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
            }
        }
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

    private static T AddOtlpHttpMetadata<T>(this T builder) where T : IEndpointConventionBuilder
    {
        builder
            .RequireAuthorization(OtlpAuthorization.PolicyName)
            .Add(b => b.Metadata.Add(new SkipStatusCodePagesAttribute()));
        return builder;
    }

    private sealed class ErrorHandlerEndpointFilter : IEndpointFilter
    {
        private readonly ILogger<ErrorHandlerEndpointFilter> _logger;

        public ErrorHandlerEndpointFilter(ILogger<ErrorHandlerEndpointFilter> logger)
        {
            _logger = logger;
        }

        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            try
            {
                return await next(context).ConfigureAwait(false);
            }
            catch (BadHttpRequestException ex)
            {
                _logger.LogError(ex, "Bad HTTP request when receiving OTLP data.");
                return Results.BadRequest(ex.Message);
            }
        }
    }

    private sealed class ProtobufResult<T> : IResult where T : IMessage
    {
        private readonly T _message;

        public ProtobufResult(T message) => _message = message;

        public async Task ExecuteAsync(HttpContext httpContext)
        {
            // This isn't very efficent but OTLP Protobuf responses are small.
            var ms = new MemoryStream();
            _message.WriteTo(ms);
            ms.Seek(0, SeekOrigin.Begin);

            httpContext.Response.ContentType = ProtobufContentType;
            await ms.CopyToAsync(httpContext.Response.Body).ConfigureAwait(false);
        }
    }

    private static async Task<T> ReadOtlpData<T>(
        HttpContext httpContext,
        Func<ReadOnlySequence<byte>, T> exporter)
    {
        const int MaxRequestSize = 1024 * 1024 * 4; // 4 MB. Matches default gRPC request limit.

        ReadResult result = default;
        try
        {
            do
            {
                result = await httpContext.Request.BodyReader.ReadAsync().ConfigureAwait(false);

                if (result.IsCanceled)
                {
                    throw new OperationCanceledException("Read call was canceled.");
                }

                if (result.Buffer.Length > MaxRequestSize)
                {
                    // Too big!
                    throw new BadHttpRequestException(
                        $"The request body was larger than the max allowed of {MaxRequestSize} bytes.",
                        StatusCodes.Status400BadRequest);
                }

                if (result.IsCompleted)
                {
                    break;
                }
                else
                {
                    httpContext.Request.BodyReader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
                }
            } while (true);

            return exporter(result.Buffer);
        }
        finally
        {
            if (!result.Equals(default(ReadResult)))
            {
                httpContext.Request.BodyReader.AdvanceTo(result.Buffer.End);
            }
        }
    }
}
