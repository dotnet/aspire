// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;
using Xunit.Sdk;

namespace Aspire.TestUtilities;

public class RequiresDockerDiscoverer : ITraitDiscoverer
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        if (!RequiresDockerAttribute.IsSupported)
        {
            yield return new KeyValuePair<string, string>(XunitConstants.Category, "failing");
        }
    }
}
