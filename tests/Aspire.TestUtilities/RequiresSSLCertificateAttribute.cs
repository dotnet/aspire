// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;

namespace Aspire.TestUtilities;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequiresSSLCertificateAttribute(string? reason = null) : Attribute, ITraitAttribute
{
    // Always supported on linux (local and CI), but only local otherwise
    public static bool IsSupported => OperatingSystem.IsLinux() || !PlatformDetection.IsRunningOnCI;

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
