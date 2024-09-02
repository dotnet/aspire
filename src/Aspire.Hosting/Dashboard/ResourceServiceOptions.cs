// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Dashboard;

internal enum ResourceServiceAuthMode
{
    // NOTE unlike ResourceClientAuthMode, there's no 'Certificate' option here.
    // The AppHost's implementation of the resource service does not support
    // certificate-based auth.

    Unsecured,
    ApiKey
}

internal sealed class ResourceServiceOptions
{
    private string? _apiKey;
    private byte[]? _apiKeyBytes;

    public ResourceServiceAuthMode? AuthMode { get; set; }

    public string? ApiKey
    {
        get => _apiKey;
        set
        {
            _apiKey = value;
            _apiKeyBytes = value is null ? null : Encoding.UTF8.GetBytes(value);
        }
    }

    internal byte[] GetApiKeyBytes()
    {
        return _apiKeyBytes ?? throw new InvalidOperationException($"AppHost:ResourceService:ApiKey is not specified in configuration.");
    }
}

internal sealed class ValidateResourceServiceOptions : IValidateOptions<ResourceServiceOptions>
{
    public ValidateOptionsResult Validate(string? name, ResourceServiceOptions options)
    {
        List<string>? errorMessages = null;

        if (options.AuthMode is ResourceServiceAuthMode.ApiKey)
        {
            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                AddError($"AppHost:ResourceService:ApiKey is required when AppHost:ResourceService:AuthMode is '{nameof(ResourceServiceAuthMode.ApiKey)}'.");
            }
        }

        return errorMessages is { Count: > 0 }
            ? ValidateOptionsResult.Fail(errorMessages)
            : ValidateOptionsResult.Success;

        void AddError(string message) => (errorMessages ??= []).Add(message);
    }
}
