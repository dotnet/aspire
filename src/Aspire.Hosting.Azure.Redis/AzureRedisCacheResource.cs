// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Cache for Redis resource.
/// </summary>
public class AzureRedisCacheResource :
    AzureProvisioningResource,
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
    /// Gets a value indicating whether the resource uses access key authentication.
    /// </summary>
    [MemberNotNullWhen(true, nameof(ConnectionStringSecretOutput))]
    public bool UseAccessKeyAuthentication => ConnectionStringSecretOutput is not null;

    // Keep for backwards compatibility
    /// <inheritdoc />
    public override ResourceAnnotationCollection Annotations => base.Annotations;

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Cache for Redis resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        Annotations.OfType<ConnectionStringAnnotation>().Last().ConnectionStringExpression;

    private ReferenceExpression GetConnectionString() =>
            UseAccessKeyAuthentication ?
                ReferenceExpression.Create($"{ConnectionStringSecretOutput}") :
                ReferenceExpression.Create($"{ConnectionStringOutput}");

    /// <param name="name">The name of the resource.</param>
    /// <param name="configureInfrastructure">Callback to configure the Azure resources.</param>
    public AzureRedisCacheResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure) : base(name, configureInfrastructure)
    {
        // We're using the ConnectionStringAnnotation to allow be overridden by the RedisCacheResource
        Annotations.Add(new ConnectionStringAnnotation(GetConnectionString));
    }
}
