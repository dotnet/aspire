// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;

namespace Aspire.TestUtilities;

/// <summary>
/// Indicates that a test requires write or export access to the certificate store.
/// Will remove this if unlocking the keychain works.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequiresCertificateStoreAccessAttribute : Attribute, ITraitAttribute
{
    public static bool IsSupported => true;

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
