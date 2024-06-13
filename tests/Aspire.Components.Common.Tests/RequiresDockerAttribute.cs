// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit.Sdk;

namespace Aspire.Components.Common.Tests;

[TraitDiscoverer("Aspire.Components.Common.Tests.RequiresDockerDiscoverer", "Aspire.Components.Common.Tests")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequiresDockerAttribute : Attribute, ITraitAttribute
{
    // A run time check is not being used here to ensure that in case
    // docker is not available for any reason, for a case where it should
    // be, then the test will fail.
    //
    // cases:
    // - Linux: always assumed that docker is installed (local, or CI)
    // - Windows: assume installed only for *local* runs
    public static bool IsSupported =>
        !OperatingSystem.IsWindows() ||
        (Environment.GetEnvironmentVariable("BUILD_BUILDID") is null && // NOT CI - build machine or helix
            Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT") is null);

    public string? Reason { get; init; }
    public RequiresDockerAttribute(string? reason = null)
    {
        Reason = reason;
    }
}
