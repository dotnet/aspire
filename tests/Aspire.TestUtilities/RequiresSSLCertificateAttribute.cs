// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit.Sdk;

namespace Aspire.TestUtilities;

[TraitDiscoverer("Aspire.TestUtilities.RequiresSSLCertificateDiscoverer", "Aspire.TestUtilities")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequiresSSLCertificateAttribute(string? reason = null) : Attribute, ITraitAttribute
{
    // Not supported on Windows CI
    public static bool IsSupported => !PlatformDetection.IsRunningOnCI || !OperatingSystem.IsWindows();

    public string? Reason { get; init; } = reason;
}
