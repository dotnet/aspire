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
    /// Initializes a new instance of the <see cref="AzureBicepResourceScope"/> class.
    /// </summary>
    /// <param name="resourceGroup">The name of the existing resource group.</param>
    /// <param name="subscription">The subscription identifier associated with the resource group.</param>
    public AzureBicepResourceScope(object resourceGroup, object subscription) : this(resourceGroup)
    {
        Subscription = subscription;
    }

    /// <summary>
    /// Represents the resource group to encode in the scope.
    /// </summary>
    public object ResourceGroup { get; } = resourceGroup;

    /// <summary>
    /// Represents the subscription to encode in the scope.
    /// </summary>
    public object? Subscription { get; }
}
