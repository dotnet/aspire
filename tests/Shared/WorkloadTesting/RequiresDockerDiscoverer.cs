// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Microsoft.DotNet.XUnitExtensions;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Aspire.Workload.Tests;

public class RequiresDockerDiscoverer : ITraitDiscoverer
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        if (!RequiresDockerTheoryAttribute.IsSupported)
        {
            yield return new KeyValuePair<string, string>(XunitConstants.Category, "failing");
        }
    }
}
