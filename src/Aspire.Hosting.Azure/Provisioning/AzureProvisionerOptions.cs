// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Aspire.Hosting.Azure.Provisioning;

internal sealed class AzureProvisionerOptions
{
    public string? SubscriptionId { get; set; }

    public string? ResourceGroup { get; set; }

    /// <summary>
    /// Gets or sets a prefix used in resource groups names created.
    /// </summary>
    public string? ResourceGroupPrefix { get; set; }

    public bool? AllowResourceGroupCreation { get; set; }

    public string? Location { get; set; }

    [AllowedValues([
        "AzureCli", "AzurePowerShell", "VisualStudio", "VisualStudioCode",
        "AzureDeveloperCli", "InteractiveBrowser", "Default"
    ])]
    public string CredentialSource { get; set; } = "Default";
}
