// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Sdk;

namespace Aspire.Workload.Tests;

[TraitDiscoverer("Aspire.Workload.Tests.RequiresDockerDiscoverer", "Aspire.Workload.Tests")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequiresDockerAttribute : Attribute, ITraitAttribute
{
    public string? Reason { get; init; }
    public RequiresDockerAttribute(string? reason = null)
    {
        Reason = reason;
    }
}
