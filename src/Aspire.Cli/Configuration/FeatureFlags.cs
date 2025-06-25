// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Configuration;

internal sealed class FeatureFlags(IConfigurationService configurationService) : IFeatureFlags
{
    public bool IsFeatureEnabled(string featureFlag)
    {
        var configKey = $"featureFlags.{featureFlag}";
        
        // Use GetAllConfigurationAsync to get the current state from files
        var allConfig = configurationService.GetAllConfigurationAsync().GetAwaiter().GetResult();
        
        if (!allConfig.TryGetValue(configKey, out var value) || string.IsNullOrEmpty(value))
        {
            return false;
        }
        
        return bool.TryParse(value, out var enabled) && enabled;
    }
}