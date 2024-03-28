// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.Authorization;

namespace Aspire.Hosting.Azure.KeyVault;

/// <summary>
/// 
/// </summary>
/// <param name="construct"></param>
/// <param name="resourceBuilder"></param>
public abstract class AzureKeyVaultDefinition(ResourceModuleConstruct construct, IResourceBuilder<AzureKeyVaultResource> resourceBuilder)
{
    /// <summary>
    /// 
    /// </summary>
    public ResourceModuleConstruct Construct { get; } = construct;

    /// <summary>
    /// 
    /// </summary>
    public IResourceBuilder<AzureKeyVaultResource> Builder { get; } = resourceBuilder;

    /// <summary>
    /// 
    /// </summary>
    public abstract global::Azure.Provisioning.KeyVaults.KeyVault KeyVault { get; protected set; }
}

/// <summary>
/// 
/// </summary>
public class DefaultAzureKeyVaultAzureKeyVaultDefinition : AzureKeyVaultDefinition
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="construct"></param>
    /// <param name="resourceBuilder"></param>
    public DefaultAzureKeyVaultAzureKeyVaultDefinition(ResourceModuleConstruct construct, IResourceBuilder<AzureKeyVaultResource> resourceBuilder) : base(construct, resourceBuilder)
    {
        KeyVault = new global::Azure.Provisioning.KeyVaults.KeyVault(construct, name: construct.Resource.Name);
        KeyVault.AddOutput("vaultUri", x => x.Properties.VaultUri);

        KeyVault.Properties.Tags["aspire-resource-name"] = construct.Resource.Name;

        var keyVaultAdministratorRoleAssignment = KeyVault.AssignRole(RoleDefinition.KeyVaultAdministrator);
        keyVaultAdministratorRoleAssignment.AssignProperty(x => x.PrincipalId, construct.PrincipalIdParameter);
        keyVaultAdministratorRoleAssignment.AssignProperty(x => x.PrincipalType, construct.PrincipalTypeParameter);
    }

    /// <summary>
    /// 
    /// </summary>
    public override global::Azure.Provisioning.KeyVaults.KeyVault KeyVault { get; protected set; }
}

/// <summary>
/// 
/// </summary>
public class MoreSecureAzureKeyVaultAzureKeyVaultDefinition : AzureKeyVaultDefinition
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="construct"></param>
    /// <param name="resourceBuilder"></param>
    public MoreSecureAzureKeyVaultAzureKeyVaultDefinition(ResourceModuleConstruct construct, IResourceBuilder<AzureKeyVaultResource> resourceBuilder) : base(construct, resourceBuilder)
    {
        KeyVault = new global::Azure.Provisioning.KeyVaults.KeyVault(construct, name: construct.Resource.Name);
        KeyVault.AddOutput("vaultUri", x => x.Properties.VaultUri);

        KeyVault.Properties.Tags["aspire-resource-name"] = construct.Resource.Name;

        var keyVaultAdministratorRoleAssignment = KeyVault.AssignRole(RoleDefinition.KeyVaultAdministrator);
        keyVaultAdministratorRoleAssignment.AssignProperty(x => x.PrincipalId, construct.PrincipalIdParameter);
        keyVaultAdministratorRoleAssignment.AssignProperty(x => x.PrincipalType, construct.PrincipalTypeParameter);

        KeyVault.AssignProperty(x => x.Properties.CreateMode, "'recover'");
    }

    /// <summary>
    /// 
    /// </summary>
    public override global::Azure.Provisioning.KeyVaults.KeyVault KeyVault { get; protected set; }
}
