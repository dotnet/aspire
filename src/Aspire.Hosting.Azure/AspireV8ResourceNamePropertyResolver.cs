// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning.Expressions;
using Azure.Provisioning.Primitives;
using Azure.Provisioning;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Uses the .NET Aspire v8.x naming scheme to provide default names for Azure resources.
/// </summary>
/// <remarks>
/// Can be used to keep consistent Azure resource names with .NET Aspire 8.x.
/// </remarks>
public sealed class AspireV8ResourceNamePropertyResolver : DynamicResourceNamePropertyResolver
{
    /// <inheritdoc/>
    public override BicepValue<string>? ResolveName(ProvisioningContext context, Resource resource, ResourceNameRequirements requirements)
    {
        var suffix = GetUniqueSuffix(context, resource);
        return BicepFunction.ToLower(BicepFunction.Take(BicepFunction.Interpolate($"{resource.ResourceName}{suffix}"), 24));
    }
}
