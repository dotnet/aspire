// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;

namespace Aspire.Cli.NuGet;

/// <summary>
/// Enables NuGet signature verification when spawning aspire-managed processes.
/// Mirrors the .NET SDK's NuGetSignatureVerificationEnabler behavior.
/// </summary>
internal static class NuGetSignatureVerificationEnabler
{
    internal const string DotNetNuGetSignatureVerification = "DOTNET_NUGET_SIGNATURE_VERIFICATION";

    /// <summary>
    /// Gets the environment variables needed for NuGet signature verification.
    /// On Linux, sets DOTNET_NUGET_SIGNATURE_VERIFICATION to "true" unless the user
    /// has explicitly set it to "false". The behavior can be disabled via the
    /// <see cref="KnownFeatures.NuGetSignatureVerificationEnabled"/> feature flag.
    /// </summary>
    public static Dictionary<string, string>? GetEnvironmentVariables(IFeatures features)
    {
        if (!OperatingSystem.IsLinux() ||
            !features.IsFeatureEnabled(
                KnownFeatures.NuGetSignatureVerificationEnabled,
                KnownFeatures.GetFeatureMetadata(KnownFeatures.NuGetSignatureVerificationEnabled)!.DefaultValue))
        {
            return null;
        }

        var value = Environment.GetEnvironmentVariable(DotNetNuGetSignatureVerification);

        // If the user explicitly set it to "false", respect that
        var effectiveValue = string.Equals(bool.FalseString, value, StringComparison.OrdinalIgnoreCase)
            ? bool.FalseString
            : bool.TrueString;

        return new Dictionary<string, string>
        {
            [DotNetNuGetSignatureVerification] = effectiveValue
        };
    }
}
