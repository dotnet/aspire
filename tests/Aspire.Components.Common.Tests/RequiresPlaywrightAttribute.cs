// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit.Sdk;

namespace Aspire.Components.Common.Tests;

[TraitDiscoverer("Aspire.Components.Common.Tests.RequiresPlaywrightDiscoverer", "Aspire.Components.Common.Tests")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequiresPlaywrightAttribute(string? reason = null) : Attribute, ITraitAttribute
{
    // This property is `true` when Playwright is expected to be installed on the machine.
    //
    // A hard-coded *expected* value is used here to ensure that CI can skip entire
    // jobs (one per test class) when Playwright is not available.
    //
    // Currently this is not supported on Linux agents on helix, and azdo build machines
    // https://github.com/dotnet/aspire/issues/4921
    public static bool IsSupported =>
        !PlatformDetection.IsRunningOnCI // Supported on local runs
        || !OperatingSystem.IsLinux() // always supported on !linux on CI
        || PlatformDetection.IsRunningOnGithubActions; // Else supported on linux/GHA only

    public string? Reason { get; init; } = reason;
}
