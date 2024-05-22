// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit.Sdk;

namespace Aspire.Components.Common.Tests;

// class level attribute

[TraitDiscoverer("Aspire.Components.Common.Tests.RequiresDockerDiscoverer", "Aspire.Workload.Tests")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequiresDockerAttribute : Attribute, ITraitAttribute
{
    public string? Reason { get; init; }
    public RequiresDockerAttribute(string? reason = null)
    {
        Reason = reason;
    }
}
