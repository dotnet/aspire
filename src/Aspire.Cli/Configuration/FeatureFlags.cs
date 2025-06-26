// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Configuration;

internal sealed class FeatureFlags(IConfiguration configuration) : IFeatureFlags
{
    public bool IsFeatureEnabled(string featureFlag, bool defaultValue)
    {
        var configKey = $"featureFlags:{featureFlag}";
        
        var value = configuration[configKey];
        
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }
        
        return bool.TryParse(value, out var enabled) && enabled;
    }
}