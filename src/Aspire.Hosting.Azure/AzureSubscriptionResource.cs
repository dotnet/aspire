// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning;
using Azure.Provisioning.Expressions;

namespace Aspire.Hosting.Azure;

/// <summary>
/// An Azure Provisioning resource that represents an Azure subscription.
/// </summary>
/// <remarks>
/// This resource allows developers building SaaS platforms to reference Azure subscriptions
/// for dynamic resource provisioning in target subscription(s).
/// </remarks>
public sealed class AzureSubscriptionResource(string name)
    : AzureProvisioningResource(name, ConfigureSubscriptionInfrastructure)
{
    /// <summary>
    /// The subscription ID.
    /// </summary>
    public BicepOutputReference SubscriptionId => new("subscriptionId", this);

    private static void ConfigureSubscriptionInfrastructure(AzureResourceInfrastructure infrastructure)
    {
        // For a subscription resource, we expose the subscription ID as an output
        // The subscription ID is available via the subscription() function in Bicep
        infrastructure.Add(new ProvisioningOutput("subscriptionId", typeof(string)) 
        { 
            Value = BicepFunction.GetSubscription().SubscriptionId 
        });
    }
}
