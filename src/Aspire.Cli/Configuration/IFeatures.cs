// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Configuration;

internal interface IFeatures
{
    bool IsFeatureEnabled(string featureFlag, bool defaultValue);

    /// <summary>
    /// Checks if a feature flag is enabled using a type-safe feature flag definition.
    /// </summary>
    /// <typeparam name="TFeatureFlag">The type of feature flag to check.</typeparam>
    /// <returns>True if the feature is enabled, false otherwise.</returns>
    bool Enabled<TFeatureFlag>() where TFeatureFlag : IFeatureFlag, new();
}