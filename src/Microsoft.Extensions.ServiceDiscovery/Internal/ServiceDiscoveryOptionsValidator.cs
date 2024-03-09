// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.ServiceDiscovery.Internal;

internal sealed class ServiceDiscoveryOptionsValidator : IValidateOptions<ServiceDiscoveryOptions>
{
    public ValidateOptionsResult Validate(string? name, ServiceDiscoveryOptions options)
    {
        if (options.AllowedSchemes is null)
        {
            return ValidateOptionsResult.Fail("At least one allowed scheme must be specified.");
        }

        return ValidateOptionsResult.Success;
    }
}

