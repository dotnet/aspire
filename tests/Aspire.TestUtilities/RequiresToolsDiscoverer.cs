// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;
using Xunit.Sdk;

namespace Aspire.TestUtilities;

public class RequiresToolsDiscoverer : ITraitDiscoverer
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        string[] executablesOnPath = (string[])traitAttribute.GetConstructorArguments().First();
        if (!executablesOnPath.All(executable => FileUtil.FindFullPathFromPath(executable) is not null))
        {
            yield return new KeyValuePair<string, string>(XunitConstants.Category, "failing");
        }
    }
}
