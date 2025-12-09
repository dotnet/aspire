// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Configuration;

internal sealed class Features(IConfiguration configuration) : IFeatures
{
    private static readonly Dictionary<Type, IFeatureFlag> s_featureFlagCache = new();

    public bool IsFeatureEnabled(string feature, bool defaultValue)
    {
        var configKey = $"features:{feature}";
        
        var value = configuration[configKey];
        
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }
        
        if (bool.TryParse(value, out var enabled))
        {
            return enabled;
        }
        
        return defaultValue;
    }

    public bool Enabled<TFeatureFlag>() where TFeatureFlag : IFeatureFlag, new()
    {
        // Cache the feature flag instance to avoid repeated allocations
        if (!s_featureFlagCache.TryGetValue(typeof(TFeatureFlag), out var featureFlag))
        {
            featureFlag = new TFeatureFlag();
            s_featureFlagCache[typeof(TFeatureFlag)] = featureFlag;
        }
        
        return IsFeatureEnabled(featureFlag.ConfigurationKey, featureFlag.DefaultValue);
    }
}