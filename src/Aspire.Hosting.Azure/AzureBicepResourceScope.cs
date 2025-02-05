// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents the scope associated with the resource.
/// </summary>
/// <param name="resourceGroup">The name of the existing resource group.</param>
public sealed class AzureBicepResourceScope(object resourceGroup)
{
    /// <summary>
    /// Represents the resource group to encode in the scope.
    /// </summary>
    public object ResourceGroup { get; } = resourceGroup;
}
