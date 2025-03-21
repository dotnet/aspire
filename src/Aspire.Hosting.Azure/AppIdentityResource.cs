// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning;
using Azure.Provisioning.Roles;

namespace Aspire.Hosting.Azure;

/// <summary>
/// An Azure Provisioning resource that represents an Azure user assigned managed identity.
/// </summary>
internal sealed class AppIdentityResource(string name)
    : AzureProvisioningResource(name, ConfigureAppIdentityInfrastructure), IAppIdentityResource
{
    public BicepOutputReference Id => new("id", this);
    public BicepOutputReference ClientId => new("clientId", this);
    public BicepOutputReference PrincipalId => new("principalId", this);
    public BicepOutputReference PrincipalName => new("principalName", this);

    private static void ConfigureAppIdentityInfrastructure(AzureResourceInfrastructure infra)
    {
        var identityName = Infrastructure.NormalizeBicepIdentifier(infra.AspireResource.Name);
        var userAssignedIdentity = new UserAssignedIdentity(identityName);
        infra.Add(userAssignedIdentity);

        infra.Add(new ProvisioningOutput("id", typeof(string)) { Value = userAssignedIdentity.Id });
        infra.Add(new ProvisioningOutput("clientId", typeof(string)) { Value = userAssignedIdentity.ClientId });
        infra.Add(new ProvisioningOutput("principalId", typeof(string)) { Value = userAssignedIdentity.PrincipalId });
        infra.Add(new ProvisioningOutput("principalName", typeof(string)) { Value = userAssignedIdentity.Name });
    }
}
