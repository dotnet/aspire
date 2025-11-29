// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Configuration;

internal sealed class Features(IConfiguration configuration) : IFeatures
{
    /// <summary>
    /// Environment variable name for enabling non-interactive SDK installation.
    /// This is a shorthand for setting features:nonInteractiveSdkInstall=true.
    /// </summary>
    private const string NonInteractiveSdkInstallEnvVar = "ASPIRE_NON_INTERACTIVE_SDK_INSTALL";

    public bool IsFeatureEnabled(string feature, bool defaultValue)
    {
        var configKey = $"features:{feature}";
        
        var value = configuration[configKey];
        
        // Special handling for NonInteractiveSdkInstall feature - also check dedicated environment variable
        if (string.IsNullOrEmpty(value) && feature == KnownFeatures.NonInteractiveSdkInstall)
        {
            value = configuration[NonInteractiveSdkInstallEnvVar];
        }
        
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }
        
        return bool.TryParse(value, out var enabled) && enabled;
    }
}