// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure;

namespace Aspire.Hosting.Azure;

/// <summary>
/// A resource that represents an Azure Key Vault.
/// </summary>
public class AzureDnsZoneResource(string name) : AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.dns-zone.bicep")
{
    /// <summary>
    /// Gets the "zoneName" output reference from the Bicep template for the Azure DNS zone resource.
    /// </summary>
    public BicepOutputReference ZoneName => new("zoneName", this);

    /// <summary>
    /// Gets the "nameservers" output reference from the Bicep template for the Azure DNS zone resource.
    /// </summary>
    public BicepOutputReference Nameservers => new("nameServers", this);
}
