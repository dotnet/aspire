// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;

namespace Aspire.Cli.Scripts.Tests.Common;

/// <summary>
/// Skips tests if the GitHub CLI (gh) is not available.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequiresGHCliAttribute : Attribute, ITraitAttribute
{
    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
    {
        if (PathLookupHelper.FindFullPathFromPath("gh") is null)
        {
            return [new KeyValuePair<string, string>(XunitConstants.Category, "failing")];
        }

        return [];
    }
}
