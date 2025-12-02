// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;

namespace Aspire.TestUtilities;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequiresGHCliAttribute : Attribute, ITraitAttribute
{
    // This property is `true` when gh cli is *expected* to be available.
    //
    // A hard-coded *expected* value is used here to ensure that gh cli
    // dependent tests *fail* if gh cli is not available/usable in an environment
    // where it is expected to be available. A run-time check would allow tests
    // to fail silently, which is not desirable.
    //
    // On CI, we always assume gh cli is available.
    // For local runs, we check if gh cli is on PATH.
    public static bool IsSupported =>
        PlatformDetection.IsRunningOnCI || FileUtil.FindFullPathFromPath("gh") is not null;

    public string? Reason { get; init; }
    public RequiresGHCliAttribute(string? reason = null)
    {
        Reason = reason;
    }

    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
    {
        if (!IsSupported)
        {
            return [new KeyValuePair<string, string>(XunitConstants.Category, "failing")];
        }

        return [];
    }
}
