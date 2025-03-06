// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;

namespace Aspire.Dashboard.Telemetry;

public interface ITelemetryResponse
{
    public HttpStatusCode StatusCode { get; }
}

public interface ITelemetryResponse<out T> : ITelemetryResponse
{
    public T? Content { get; }
}

public class TelemetryResponse(HttpStatusCode statusCode) : ITelemetryResponse
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

public class TelemetryResponse<T>(HttpStatusCode statusCode, T? result) : ITelemetryResponse<T>
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public T? Content { get; } = result;
}

public record TelemetryEnabledResponse(bool IsEnabled);

public record StartOperationResponse(string OperationId, TelemetryEventCorrelation Correlation);
