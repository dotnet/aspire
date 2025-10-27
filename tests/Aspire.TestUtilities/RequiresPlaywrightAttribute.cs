// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;

namespace Aspire.TestUtilities;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequiresPlaywrightAttribute(string? reason = null) : Attribute, ITraitAttribute
{
    private static bool? s_isSupported;

    // This property is `true` when Playwright is expected to be installed on the machine.
    //
    // A hard-coded *expected* value is used here to ensure that CI can skip entire
    // jobs (one per test class) when Playwright is not available.
    //
    // Currently this is not supported on Linux agents on helix, and azdo build machines
    // https://github.com/dotnet/aspire/issues/4921
    public static bool IsSupported
    {
        get
        {
            s_isSupported ??= GetIsSupported();
            return s_isSupported.Value;
        }
    }
    private static bool GetIsSupported()
    {
        // Setting PLAYWRIGHT_INSTALLED environment variable takes precedence
        if (Environment.GetEnvironmentVariable("PLAYWRIGHT_INSTALLED") is var playwrightInstalled && !string.IsNullOrEmpty(playwrightInstalled))
        {
            if (bool.TryParse(playwrightInstalled, out var isInstalled))
            {
                return isInstalled;
            }
        }

        return !PlatformDetection.IsRunningOnCI // Supported on local runs
            || !OperatingSystem.IsLinux() // always supported on !linux on CI
            || PlatformDetection.IsRunningOnGithubActions;
    }

    public string? Reason { get; init; } = reason;

    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
    {
        if (!IsSupported)
        {
            return [new KeyValuePair<string, string>(XunitConstants.Category, "failing")];
        }

        return [];
    }
}
