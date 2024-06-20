// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Net.Http.Headers;
using System.Reflection;
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

    public class MessageBindable<TMessage> : IBindableFromHttpContext<MessageBindable<TMessage>> where TMessage : IMessage<TMessage>, new()
    {
        public static readonly MessageBindable<TMessage> Empty = new MessageBindable<TMessage>();

        public TMessage? Message { get; private set; }

        public static async ValueTask<MessageBindable<TMessage>?> BindAsync(HttpContext context, ParameterInfo parameter)
        {
            switch (GetKnownContentType(context.Request.ContentType, out var charSet))
            {
                case KnownContentType.Protobuf:
                    try
                    {
                        var message = await ReadOtlpData(context, data =>
                        {
                            var message = new TMessage();
                            message.MergeFrom(data);
                            return message;
                        }).ConfigureAwait(false);

                        return new MessageBindable<TMessage> { Message = message };
                    }
                    catch (BadHttpRequestException ex)
                    {
                        context.Response.StatusCode = ex.StatusCode;
                        return Empty;
                    }
                case KnownContentType.Json:
                default:
                    context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                    return Empty;
            }
        }
    }

    public static void MapHttpOtlpApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/v1")
            .AddOtlpHttpMetadata();

        group.MapPost("logs", LogsEndpoint);
        group.MapPost("traces", TracesEndpoint);
        group.MapPost("metrics", MetricsEndpoint);

        IResult LogsEndpoint(MessageBindable<ExportLogsServiceRequest> request, OtlpLogsService service)
        {
            if (request.Message == null)
            {
                return Results.Empty;
            }
            return new ProtobufResult<ExportLogsServiceResponse>(service.Export(request.Message));
        }
        IResult TracesEndpoint(MessageBindable<ExportTraceServiceRequest> request, OtlpTraceService service)
        {
            if (request.Message == null)
            {
                return Results.Empty;
            }
            return new ProtobufResult<ExportTraceServiceResponse>(service.Export(request.Message));
        }
        IResult MetricsEndpoint(MessageBindable<ExportMetricsServiceRequest> request, OtlpMetricsService service)
        {
            if (request.Message == null)
            {
                return Results.Empty;
            }
            return new ProtobufResult<ExportMetricsServiceResponse>(service.Export(request.Message));
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
