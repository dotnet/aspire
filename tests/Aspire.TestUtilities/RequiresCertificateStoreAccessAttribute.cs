// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;

namespace Aspire.TestUtilities;

/// <summary>
/// Indicates that a test requires write or export access to the certificate store.
/// This is not supported on macOS currently as keychain requires user interaction for these operations.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequiresCertificateStoreAccessAttribute : Attribute, ITraitAttribute
{
    // Returns true if a valid ASP.NET Core development certificate is found in the current user's certificate store.
    public static bool IsSupported => !OperatingSystem.IsMacOS(); // Can't get write or export access to the keychain in the CI currently

    public string? Reason { get; init; }
    public RequiresCertificateStoreAccessAttribute(string? reason = null)
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
