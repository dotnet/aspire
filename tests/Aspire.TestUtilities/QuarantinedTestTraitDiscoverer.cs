// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit.Sdk;

// Do not change this namespace without changing the usage in QuarantinedTestAttribute
namespace Aspire.TestUtilities;

public sealed class QuarantinedTestTraitDiscoverer : ITraitDiscoverer
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        if (traitAttribute is ReflectionAttributeInfo attribute && attribute.Attribute is QuarantinedTestAttribute)
        {
            yield return new KeyValuePair<string, string>("quarantined", "true");
        }
        else
        {
            throw new InvalidOperationException("The 'QuarantinedTest' attribute is only supported via reflection.");
        }
    }
}
