// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Maui.DevTunnels;

/// <summary>
/// Resolves the OTLP exporter scheme and port from Aspire dashboard configuration.
/// Priority: unified endpoint -> HTTP-specific -> gRPC-specific -> defaults.
/// </summary>
internal static class OtlpEndpointResolver
{
    public static (string Scheme, int Port) Resolve(IConfiguration configuration)
    {
        var unifiedUrl = configuration["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"]; // launchSettings uses this key
        var httpUrl = configuration["ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL"]; // newer split key
        var grpcUrl = configuration["ASPIRE_DASHBOARD_OTLP_GRPC_ENDPOINT_URL"]; // newer split key

        if (Uri.TryCreate(unifiedUrl, UriKind.Absolute, out var unified))
        {
            return (unified.Scheme, unified.IsDefaultPort ? (unified.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? 443 : 80) : unified.Port);
        }
        if (Uri.TryCreate(httpUrl, UriKind.Absolute, out var http))
        {
            return (http.Scheme, http.IsDefaultPort ? (http.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? 443 : 80) : http.Port);
        }
        if (Uri.TryCreate(grpcUrl, UriKind.Absolute, out var grpc))
        {
            // gRPC exporter commonly uses 4317 as the default when unspecified.
            return (grpc.Scheme, grpc.IsDefaultPort ? 4317 : grpc.Port);
        }
        return ("http", 18889); // Fallback defaults (match dashboard defaults for self-hosted collector)
    }
}
