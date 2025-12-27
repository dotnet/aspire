// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;

namespace Aspire.TestUtilities;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequiresUnixSystemAttribute : Attribute, ITraitAttribute
{
    public static bool IsSupported => !OperatingSystem.IsWindows();

    public string? Reason { get; init; }

    public RequiresUnixSystemAttribute(string? reason = null)
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
