// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit.Sdk;

namespace Aspire.Components.Common.Tests;

[TraitDiscoverer("Aspire.Components.Common.Tests.RequiresDockerDiscoverer", "Aspire.Components.Common.Tests")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequiresDockerAttribute : Attribute, ITraitAttribute
{
    // This property is `true` when docker is *expected* to be available.
    //
    // A hard-coded *expected* value is used here to ensure that docker
    // dependent tests *fail* if docker is not available/usable in an environment
    // where it is expected to be available. A run-time check would allow tests
    // to fail silently, which is not desirable.
    //
    // scenarios:
    // - Windows: assume installed only for *local* runs as docker isn't supported on CI yet
    //                - https://github.com/dotnet/aspire/issues/4291
    // - Linux - Local, or CI: always assume that docker is installed
    public static bool IsSupported =>
        !OperatingSystem.IsWindows() ||
        !PlatformDetection.IsRunningOnCI;

    public string? Reason { get; init; }
    public RequiresDockerAttribute(string? reason = null)
    {
        Reason = reason;
    }
}
