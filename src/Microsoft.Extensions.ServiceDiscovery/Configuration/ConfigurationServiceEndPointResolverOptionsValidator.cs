// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.ServiceDiscovery.Configuration;

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
