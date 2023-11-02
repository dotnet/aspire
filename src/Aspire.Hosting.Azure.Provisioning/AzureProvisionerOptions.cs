// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.Provisioning;

internal sealed class AzureProvisionerOptions
{
    public string? SubscriptionId { get; set; }

    public string? ResourceGroup { get; set; }

    public bool? AllowResourceGroupCreation { get; set; }

    public string? Location { get; set; }
}
