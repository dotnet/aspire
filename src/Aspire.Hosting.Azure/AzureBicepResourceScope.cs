// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents the scope associated with the resource.
/// </summary>
public sealed class AzureBicepResourceScope
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBicepResourceScope"/> class with a resource group.
    /// </summary>
    /// <param name="resourceGroup">The name of the existing resource group.</param>
    public AzureBicepResourceScope(object resourceGroup)
    {
        ArgumentNullException.ThrowIfNull(resourceGroup);
        ResourceGroup = resourceGroup;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBicepResourceScope"/> class with both resource group and subscription.
    /// </summary>
    /// <param name="resourceGroup">The name of the existing resource group.</param>
    /// <param name="subscription">The subscription identifier associated with the resource group.</param>
    public AzureBicepResourceScope(object resourceGroup, object subscription) : this(resourceGroup)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        Subscription = subscription;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBicepResourceScope"/> class for subscription-level resources.
    /// </summary>
    /// <param name="subscription">The subscription identifier for subscription-level resources.</param>
    /// <param name="subscriptionOnly">Must be true to indicate this is a subscription-only scope.</param>
    public AzureBicepResourceScope(object subscription, bool subscriptionOnly)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        if (!subscriptionOnly)
        {
            throw new ArgumentException("subscriptionOnly parameter must be true when creating subscription-only scope.", nameof(subscriptionOnly));
        }
        Subscription = subscription;
    }

    /// <summary>
    /// Represents the resource group to encode in the scope.
    /// </summary>
    public object? ResourceGroup { get; }

    /// <summary>
    /// Represents the subscription to encode in the scope.
    /// </summary>
    public object? Subscription { get; }
}
