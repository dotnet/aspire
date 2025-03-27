// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit.Sdk;

namespace Aspire.TestUtilities;

[TraitDiscoverer("Aspire.TestUtilities.RequiresToolsDiscoverer", "Aspire.TestUtilities")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequiresToolsAttribute : Attribute, ITraitAttribute
{
    public RequiresToolsAttribute(string[] executablesOnPath)
    {
        if (executablesOnPath.Length == 0)
        {
            throw new ArgumentException("At least one executable must be provided", nameof(executablesOnPath));
        }
    }
}
