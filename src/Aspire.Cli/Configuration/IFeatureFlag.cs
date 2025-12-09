// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Configuration;

/// <summary>
/// Represents a feature flag with its configuration key and default value.
/// </summary>
internal interface IFeatureFlag
{
    /// <summary>
    /// Gets the configuration key used to look up the feature flag value.
    /// </summary>
    string ConfigurationKey { get; }

    /// <summary>
    /// Gets the default value for the feature flag when not explicitly configured.
    /// </summary>
    bool DefaultValue { get; }
}
