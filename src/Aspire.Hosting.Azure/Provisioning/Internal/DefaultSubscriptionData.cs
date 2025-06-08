// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.ResourceManager.Resources;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Default implementation of <see cref="ISubscriptionData"/>.
/// </summary>
internal sealed class DefaultSubscriptionData(SubscriptionData subscriptionData) : ISubscriptionData
{
    public ResourceIdentifier Id => subscriptionData.Id;
    public string? DisplayName => subscriptionData.DisplayName;
    public Guid? TenantId => subscriptionData.TenantId;
}