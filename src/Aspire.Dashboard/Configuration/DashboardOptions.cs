// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Aspire.Hosting;

namespace Aspire.Dashboard.Configuration;

public sealed class DashboardOptions
{
    public string? ApplicationName { get; set; }
    public OtlpOptions Otlp { get; set; } = new OtlpOptions();
    public FrontendOptions Frontend { get; set; } = new FrontendOptions();
    public ResourceServiceClientOptions ResourceServiceClient { get; set; } = new ResourceServiceClientOptions();
    public TelemetryLimitOptions TelemetryLimits { get; set; } = new TelemetryLimitOptions();
}

// Don't set values after validating/parsing options.
public sealed class ResourceServiceClientOptions
{
    private Uri? _parsedUrl;

    public string? Url { get; set; }
    public ResourceClientAuthMode? AuthMode { get; set; }
    public ResourceServiceClientCertificateOptions ClientCertificates { get; set; } = new ResourceServiceClientCertificateOptions();

    public Uri? GetUri() => _parsedUrl;

    internal bool TryParseOptions([NotNullWhen(false)] out string? errorMessage)
    {
        if (!string.IsNullOrEmpty(Url))
        {
            if (!Uri.TryCreate(Url, UriKind.Absolute, out _parsedUrl))
            {
                errorMessage = $"Failed to parse resource service client endpoint URL '{Url}'.";
                return false;
            }
        }

        errorMessage = null;
        return true;
    }
}

public sealed class ResourceServiceClientCertificateOptions
{
    public DashboardClientCertificateSource? Source { get; set; }
    public string? FilePath { get; set; }
    public string? Password { get; set; }
    public string? Subject { get; set; }
    public string? Store { get; set; }
    public StoreLocation? Location { get; set; }
}

// Don't set values after validating/parsing options.
public sealed class OtlpOptions
{
    private Uri? _parsedEndpointUrl;
    private byte[]? _primaryApiKeyBytes;
    private byte[]? _secondaryApiKeyBytes;

    public string? PrimaryApiKey { get; set; }
    public string? SecondaryApiKey { get; set; }
    public OtlpAuthMode? AuthMode { get; set; }
    public string? EndpointUrl { get; set; }

    public Uri GetEndpointUri()
    {
        Debug.Assert(_parsedEndpointUrl is not null, "Should have been parsed during validation.");
        return _parsedEndpointUrl;
    }

    public byte[] GetPrimaryApiKeyBytes()
    {
        Debug.Assert(_primaryApiKeyBytes is not null, "Should have been parsed during validation.");
        return _primaryApiKeyBytes;
    }

    public byte[]? GetSecondaryApiKeyBytes() => _secondaryApiKeyBytes;

    internal bool TryParseOptions([NotNullWhen(false)] out string? errorMessage)
    {
        if (string.IsNullOrEmpty(EndpointUrl))
        {
            errorMessage = $"OTLP endpoint URL is not configured. Specify a {DashboardConfigNames.DashboardOtlpUrlName.EnvVarName} value.";
            return false;
        }
        else
        {
            if (!Uri.TryCreate(EndpointUrl, UriKind.Absolute, out _parsedEndpointUrl))
            {
                errorMessage = $"Failed to parse OTLP endpoint URL '{EndpointUrl}'.";
                return false;
            }
        }

        _primaryApiKeyBytes = PrimaryApiKey != null ? Encoding.UTF8.GetBytes(PrimaryApiKey) : null;
        _secondaryApiKeyBytes = SecondaryApiKey != null ? Encoding.UTF8.GetBytes(SecondaryApiKey) : null;

        errorMessage = null;
        return true;
    }
}

// Don't set values after validating/parsing options.
public sealed class FrontendOptions
{
    private List<Uri>? _parsedEndpointUrls;
    private byte[]? _browserTokenBytes;

    public string? EndpointUrls { get; set; }
    public FrontendAuthMode? AuthMode { get; set; }
    public string? BrowserToken { get; set; }

    public byte[]? GetBrowserTokenBytes() => _browserTokenBytes;

    public IReadOnlyList<Uri> GetEndpointUris()
    {
        Debug.Assert(_parsedEndpointUrls is not null, "Should have been parsed during validation.");
        return _parsedEndpointUrls;
    }

    internal bool TryParseOptions([NotNullWhen(false)] out string? errorMessage)
    {
        if (string.IsNullOrEmpty(EndpointUrls))
        {
            errorMessage = "One or more frontend endpoint URLs are not configured. Specify a Dashboard:Frontend:EndpointUrls value.";
            return false;
        }
        else
        {
            var parts = EndpointUrls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var uris = new List<Uri>(parts.Length);
            foreach (var part in parts)
            {
                if (!Uri.TryCreate(part, UriKind.Absolute, out var uri))
                {
                    errorMessage = $"Failed to parse frontend endpoint URLs '{EndpointUrls}'.";
                    return false;
                }

                uris.Add(uri);
            }
            _parsedEndpointUrls = uris;
        }

        _browserTokenBytes = BrowserToken != null ? Encoding.UTF8.GetBytes(BrowserToken) : null;

        errorMessage = null;
        return true;
    }
}

public sealed class TelemetryLimitOptions
{
    public int MaxLogCount { get; set; } = 10_000;
    public int MaxTraceCount { get; set; } = 10_000;
    public int MaxMetricsCount { get; set; } = 50_000; // Allows for 1 metric point per second for over 12 hours.
    public int MaxAttributeCount { get; set; } = 128;
    public int MaxAttributeLength { get; set; } = int.MaxValue;
    public int MaxSpanEventCount { get; set; } = int.MaxValue;
}
