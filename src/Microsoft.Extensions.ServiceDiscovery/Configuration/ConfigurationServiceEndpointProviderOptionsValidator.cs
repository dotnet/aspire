// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.ServiceDiscovery.Configuration;

internal sealed class ConfigurationServiceEndpointProviderOptionsValidator : IValidateOptions<ConfigurationServiceEndpointProviderOptions>
{
    public ValidateOptionsResult Validate(string? name, ConfigurationServiceEndpointProviderOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.SectionName))
        {
            return ValidateOptionsResult.Fail($"{nameof(options.SectionName)} must not be null or empty.");
        }

        if (options.ShouldApplyHostNameMetadata is null)
        {
            return ValidateOptionsResult.Fail($"{nameof(options.ShouldApplyHostNameMetadata)} must not be null.");
        }

        return ValidateOptionsResult.Success;
    }
}
