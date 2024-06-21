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
    private byte[]? _apiKeyBytes;

    public string? Url { get; set; }
    public ResourceClientAuthMode? AuthMode { get; set; }
    public ResourceServiceClientCertificateOptions ClientCertificates { get; set; } = new ResourceServiceClientCertificateOptions();
    public string? ApiKey { get; set; }

    public Uri? GetUri() => _parsedUrl;

    internal byte[] GetApiKeyBytes() => _apiKeyBytes ?? throw new InvalidOperationException($"{nameof(ApiKey)} is not available.");

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

        _apiKeyBytes = ApiKey != null ? Encoding.UTF8.GetBytes(ApiKey) : null;

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
    private Uri? _parsedGrpcEndpointUrl;
    private Uri? _parsedHttpEndpointUrl;
    private byte[]? _primaryApiKeyBytes;
    private byte[]? _secondaryApiKeyBytes;

    public string? PrimaryApiKey { get; set; }
    public string? SecondaryApiKey { get; set; }
    public OtlpAuthMode? AuthMode { get; set; }
    public string? GrpcEndpointUrl { get; set; }

    public string? HttpEndpointUrl { get; set; }

    public Uri? GetGrpcEndpointUri()
    {
        return _parsedGrpcEndpointUrl;
    }

    public Uri? GetHttpEndpointUri()
    {
        return _parsedHttpEndpointUrl;
    }

    public byte[] GetPrimaryApiKeyBytes()
    {
        Debug.Assert(_primaryApiKeyBytes is not null, "Should have been parsed during validation.");
        return _primaryApiKeyBytes;
    }

    public byte[]? GetSecondaryApiKeyBytes() => _secondaryApiKeyBytes;

    internal bool TryParseOptions([NotNullWhen(false)] out string? errorMessage)
    {
        if (string.IsNullOrEmpty(GrpcEndpointUrl) && string.IsNullOrEmpty(HttpEndpointUrl))
        {
            errorMessage = $"Neither OTLP/gRPC or OTLP/HTTP endpoint URLs are configured. Specify either a {DashboardConfigNames.DashboardOtlpGrpcUrlName.EnvVarName} or {DashboardConfigNames.DashboardOtlpHttpUrlName.EnvVarName} value.";
            return false;
        }

        if (!string.IsNullOrEmpty(GrpcEndpointUrl) && !Uri.TryCreate(GrpcEndpointUrl, UriKind.Absolute, out _parsedGrpcEndpointUrl))
        {
            errorMessage = $"Failed to parse OTLP gRPC endpoint URL '{GrpcEndpointUrl}'.";
            return false;
        }

        if (!string.IsNullOrEmpty(HttpEndpointUrl) && !Uri.TryCreate(HttpEndpointUrl, UriKind.Absolute, out _parsedHttpEndpointUrl))
        {
            errorMessage = $"Failed to parse OTLP HTTP endpoint URL '{HttpEndpointUrl}'.";
            return false;
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
    public OpenIdConnectOptions OpenIdConnect { get; set; } = new OpenIdConnectOptions();

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

// Don't set values after validating/parsing options.
public sealed class OpenIdConnectOptions
{
    private string[]? _nameClaimTypes;
    private string[]? _usernameClaimTypes;

    public string NameClaimType { get; set; } = "name";
    public string UsernameClaimType { get; set; } = "preferred_username";

    /// <summary>
    /// Gets the optional name of a claim that users authenticated via OpenID Connect are required to have.
    /// If specified, users without this claim will be rejected. If <see cref="RequiredClaimValue"/>
    /// is also specified, then the value of this claim must also match <see cref="RequiredClaimValue"/>.
    /// </summary>
    public string RequiredClaimType { get; set; } = "";

    /// <summary>
    /// Gets the optional value of the <see cref="RequiredClaimType"/> claim for users authenticated via
    /// OpenID Connect. If specified, users not having this value for the corresponding claim type are
    /// rejected.
    /// </summary>
    public string RequiredClaimValue { get; set; } = "";

    public string[] GetNameClaimTypes()
    {
        Debug.Assert(_nameClaimTypes is not null, "Should have been parsed during validation.");
        return _nameClaimTypes;
    }

    public string[] GetUsernameClaimTypes()
    {
        Debug.Assert(_usernameClaimTypes is not null, "Should have been parsed during validation.");
        return _usernameClaimTypes;
    }

    internal bool TryParseOptions([NotNullWhen(false)] out IEnumerable<string>? errorMessages)
    {
        List<string>? messages = null;

        if (string.IsNullOrWhiteSpace(NameClaimType))
        {
            messages ??= [];
            messages.Add("OpenID Connect claim type for name not configured. Specify a Dashboard:Frontend:OpenIdConnect:NameClaimType value.");
        }
        else
        {
            _nameClaimTypes = NameClaimType.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        if (string.IsNullOrWhiteSpace(UsernameClaimType))
        {
            messages ??= [];
            messages.Add("OpenID Connect claim type for username not configured. Specify a Dashboard:Frontend:OpenIdConnect:UsernameClaimType value.");
        }
        else
        {
            _usernameClaimTypes = UsernameClaimType.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        errorMessages = messages;

        return messages is null;
    }
}
