// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Cache for Redis resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="configureConstruct">Callback to populate the construct with Azure resources.</param>
public class AzureRedisCacheResource(string name, Action<ResourceModuleConstruct> configureConstruct) :
    AzureConstructResource(name, configureConstruct),
    IResourceWithConnectionString
{
    /// <summary>
    /// Gets the "connectionString" output reference from the bicep template for the Azure Redis resource.
    ///
    /// This is used when Entra ID authentication is used. The connection string is an output of the bicep template.
    /// </summary>
    private BicepOutputReference ConnectionStringOutput => new("connectionString", this);

    /// <summary>
    /// Gets the "connectionString" secret output reference from the bicep template for the Azure Redis resource.
    ///
    /// This is set when access key authentication is used. The connection string is stored in a secret in the Azure Key Vault.
    /// </summary>
    internal BicepSecretOutputReference? ConnectionStringSecretOutput { get; set; }

    /// <summary>
    /// Gets the inner Redis resource.
    /// 
    /// This is set when RunAsContainer is called on the AzureRedisCacheResource resource to create a local Redis container.
    /// </summary>
    internal RedisResource? InnerResource { get; set; }

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Cache for Redis resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        InnerResource?.ConnectionStringExpression ??
            (ConnectionStringSecretOutput is not null ?
                ReferenceExpression.Create($"{ConnectionStringSecretOutput}") :
                ReferenceExpression.Create($"{ConnectionStringOutput}"));
}
