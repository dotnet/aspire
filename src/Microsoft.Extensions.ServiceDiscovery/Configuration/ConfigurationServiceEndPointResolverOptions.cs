// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

/// <summary>
/// Options for <see cref="ConfigurationServiceEndPointResolver"/>.
/// </summary>
public sealed class ConfigurationServiceEndPointResolverOptions
{
    /// <summary>
    /// The name of the configuration section which contains service endpoints. Defaults to <c>"Services"</c>.
    /// </summary>
    public string SectionName { get; set; } = "Services";

    /// <summary>
    /// Gets or sets a delegate used to determine whether to apply host name metadata to each resolved endpoint. Defaults to <c>false</c>.
    /// </summary>
    public Func<ServiceEndPoint, bool> ApplyHostNameMetadata { get; set; } = _ => false;
}

internal sealed class ConfigurationServiceEndPointResolverOptionsValidator : IValidateOptions<ConfigurationServiceEndPointResolverOptions>
{
    public ValidateOptionsResult Validate(string? name, ConfigurationServiceEndPointResolverOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.SectionName))
        {
            return ValidateOptionsResult.Fail($"{nameof(options.SectionName)} must not be null or empty.");
        }

        if (options.ApplyHostNameMetadata is null)
        {
            return ValidateOptionsResult.Fail($"{nameof(options.ApplyHostNameMetadata)} must not be null.");
        }

        return ValidateOptionsResult.Success;
    }
}
