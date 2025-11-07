// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Maui.Otlp;

/// <summary>
/// Resolves OTLP endpoint configuration (scheme and port) from standard OTLP environment variables.
/// </summary>
internal static class OtlpEndpointResolver
{
    /// <summary>
    /// Resolves the OTLP endpoint scheme and port from configuration.
    /// </summary>
    /// <param name="configuration">The configuration to read from.</param>
    /// <returns>A tuple of (scheme, port) for the OTLP endpoint.</returns>
    /// <remarks>
    /// Priority order:
    /// 1. Unified endpoint (OTEL_EXPORTER_OTLP_ENDPOINT)
    /// 2. HTTP-specific endpoint (ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL)
    /// 3. gRPC-specific endpoint (ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL)
    /// 4. Default: http://localhost:18889 (gRPC default)
    /// </remarks>
    public static (string Scheme, int Port) Resolve(IConfiguration configuration)
    {
        // Try unified endpoint first
        var unifiedEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(unifiedEndpoint) && Uri.TryCreate(unifiedEndpoint, UriKind.Absolute, out var unifiedUri))
        {
            return (unifiedUri.Scheme, unifiedUri.Port);
        }

        // Try HTTP-specific endpoint
        var httpEndpoint = configuration["ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL"];
        if (!string.IsNullOrWhiteSpace(httpEndpoint) && Uri.TryCreate(httpEndpoint, UriKind.Absolute, out var httpUri))
        {
            return (httpUri.Scheme, httpUri.Port);
        }

        // Try gRPC-specific endpoint (most common for Aspire dashboard)
        var grpcEndpoint = configuration["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"];
        if (!string.IsNullOrWhiteSpace(grpcEndpoint) && Uri.TryCreate(grpcEndpoint, UriKind.Absolute, out var grpcUri))
        {
            return (grpcUri.Scheme, grpcUri.Port);
        }

        // Default to gRPC endpoint on port 18889 (Aspire dashboard default)
        return ("http", 18889);
    }
}
