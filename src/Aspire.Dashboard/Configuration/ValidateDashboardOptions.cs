// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Configuration;

public sealed class ValidateDashboardOptions : IValidateOptions<DashboardOptions>
{
    public ValidateOptionsResult Validate(string? name, DashboardOptions options)
    {
        var errorMessages = new List<string>();

        if (!options.Frontend.TryParseOptions(out var frontendParseErrorMessage))
        {
            errorMessages.Add(frontendParseErrorMessage);
        }

        switch (options.Frontend.AuthMode)
        {
            case FrontendAuthMode.Unsecured:
                break;
            case FrontendAuthMode.OpenIdConnect:
                if (!options.Frontend.OpenIdConnect.TryParseOptions(out var messages))
                {
                    errorMessages.AddRange(messages);
                }
                break;
            case FrontendAuthMode.BrowserToken:
                if (string.IsNullOrEmpty(options.Frontend.BrowserToken))
                {
                    errorMessages.Add($"BrowserToken is required when frontend authentication mode is browser token. Specify a {DashboardConfigNames.DashboardFrontendBrowserTokenName.ConfigKey} value.");
                }
                break;
            case null:
                errorMessages.Add($"Frontend endpoint authentication is not configured. Either specify {DashboardConfigNames.DashboardUnsecuredAllowAnonymousName.ConfigKey}=true, or specify {DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey}. Possible values: {string.Join(", ", typeof(FrontendAuthMode).GetEnumNames())}");
                break;
            default:
                errorMessages.Add($"Unexpected frontend authentication mode: {options.Otlp.AuthMode}");
                break;
        }

        if (!options.Otlp.TryParseOptions(out var otlpParseErrorMessage))
        {
            errorMessages.Add(otlpParseErrorMessage);
        }

        switch (options.Otlp.AuthMode)
        {
            case OtlpAuthMode.Unsecured:
                break;
            case OtlpAuthMode.ApiKey:
                if (string.IsNullOrEmpty(options.Otlp.PrimaryApiKey))
                {
                    errorMessages.Add("PrimaryApiKey is required when OTLP authentication mode is API key. Specify a Dashboard:Otlp:PrimaryApiKey value.");
                }
                break;
            case OtlpAuthMode.ClientCertificate:
                break;
            case null:
                errorMessages.Add($"OTLP endpoint authentication is not configured. Either specify {DashboardConfigNames.DashboardUnsecuredAllowAnonymousName.ConfigKey}=true, or specify {DashboardConfigNames.DashboardOtlpAuthModeName.ConfigKey}. Possible values: {string.Join(", ", typeof(OtlpAuthMode).GetEnumNames())}");
                break;
            default:
                errorMessages.Add($"Unexpected OTLP authentication mode: {options.Otlp.AuthMode}");
                break;
        }

        if (!options.ResourceServiceClient.TryParseOptions(out var resourceServiceClientParseErrorMessage))
        {
            errorMessages.Add(resourceServiceClientParseErrorMessage);
        }

        // Only validate resource service configuration if we have a URI to connect to.
        // If we do not, then the dashboard will run without resources, but still show OTEL data.
        if (options.ResourceServiceClient.GetUri() != null)
        {
            switch (options.ResourceServiceClient.AuthMode)
            {
                case ResourceClientAuthMode.Unsecured:
                    break;
                case ResourceClientAuthMode.ApiKey:
                    if (string.IsNullOrWhiteSpace(options.ResourceServiceClient.ApiKey))
                    {
                        errorMessages.Add($"{DashboardConfigNames.ResourceServiceClientAuthModeName.ConfigKey} is \"{nameof(ResourceClientAuthMode.ApiKey)}\", but no {DashboardConfigNames.ResourceServiceClientApiKeyName.ConfigKey} is configured.");
                    }
                    break;
                case ResourceClientAuthMode.Certificate:
                    switch (options.ResourceServiceClient.ClientCertificates.Source)
                    {
                        case DashboardClientCertificateSource.File:
                            if (string.IsNullOrEmpty(options.ResourceServiceClient.ClientCertificates.FilePath))
                            {
                                errorMessages.Add("Dashboard:ResourceServiceClient:ClientCertificate:Source is \"File\", but no Dashboard:ResourceServiceClient:ClientCertificate:FilePath is configured.");
                            }
                            break;
                        case DashboardClientCertificateSource.KeyStore:
                            if (string.IsNullOrEmpty(options.ResourceServiceClient.ClientCertificates.Subject))
                            {
                                errorMessages.Add("Dashboard:ResourceServiceClient:ClientCertificate:Source is \"KeyStore\", but no Dashboard:ResourceServiceClient:ClientCertificate:Subject is configured.");
                            }
                            break;
                        case null:
                            errorMessages.Add($"The resource service client is configured to use certificates, but no certificate source is specified. Specify Dashboard:ResourceServiceClient:ClientCertificate:Source. Possible values: {string.Join(", ", typeof(DashboardClientCertificateSource).GetEnumNames())}");
                            break;
                        default:
                            errorMessages.Add($"Unexpected resource service client certificate source: {options.ResourceServiceClient.ClientCertificates.Source}");
                            break;
                    }
                    break;
                case null:
                    errorMessages.Add($"Resource service client authentication is not configured. Specify {DashboardConfigNames.ResourceServiceClientAuthModeName.ConfigKey}. Possible values: {string.Join(", ", typeof(ResourceClientAuthMode).GetEnumNames())}");
                    break;
                default:
                    errorMessages.Add($"Unexpected resource service client authentication mode: {options.ResourceServiceClient.AuthMode}");
                    break;
            }
        }

        return errorMessages.Count > 0
            ? ValidateOptionsResult.Fail(errorMessages)
            : ValidateOptionsResult.Success;
    }
}
