// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.KeyVault;
using Azure.Provisioning.Primitives;

namespace Aspire.Hosting.Azure;

/// <summary>
/// A resource that represents an Azure Key Vault.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="configureInfrastructure">Callback to configure the Azure resources.</param>
public class AzureKeyVaultResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure)
    : AzureProvisioningResource(name, configureInfrastructure), IResourceWithConnectionString, IKeyVaultResource
{
    /// <summary>
    /// Gets the "vaultUri" output reference for the Azure Key Vault resource.
    /// </summary>
    public BicepOutputReference VaultUri => new("vaultUri", this);

    /// <summary>
    /// Gets the "name" output reference for the Azure Key Vault resource.
    /// </summary>
    public BicepOutputReference NameOutputReference => new("name", this);

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Key Vault resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{VaultUri}");

    /// <summary>
    /// The secrets for the Azure Key Vault resource. Used in run mode to resolve
    /// </summary>
    public Dictionary<string, string> Secrets { get; } = [];

    IDictionary<string, string> IKeyVaultResource.Secrets => Secrets;

    /// <summary>
    /// Gets a secret reference for the specified secret name.
    /// </summary>
    /// <param name="secretName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public IKeyVaultSecretReference GetSecretReference(string secretName)
    {
        ArgumentException.ThrowIfNullOrEmpty(secretName, nameof(secretName));

        return new AzureKeyVaultSecretReference(secretName, this);
    }

    /// <inheritdoc/>
    public override ProvisionableResource AddAsExistingResource(AzureResourceInfrastructure infra)
    {
        var store = KeyVaultService.FromExisting(this.GetBicepIdentifier());
        store.Name = NameOutputReference.AsProvisioningParameter(infra);
        infra.Add(store);
        return store;
    }
}

/// <summary>
/// Represents a reference to a secret in an Azure Key Vault resource.
/// </summary>
/// <param name="secretName">The name of the secret.</param>
/// <param name="azureKeyVaultResource">The Azure Key Vault resource.</param>
public sealed class AzureKeyVaultSecretReference(string secretName, IKeyVaultResource azureKeyVaultResource) : IKeyVaultSecretReference, IValueProvider, IManifestExpressionProvider
{
    /// <summary>
    /// Gets the name of the secret.
    /// </summary>
    public string SecretName => secretName;

    /// <summary>
    /// Gets the Azure Key Vault resource.
    /// </summary>
    public IKeyVaultResource KeyVaultResource => azureKeyVaultResource;

    string IManifestExpressionProvider.ValueExpression => $"{{{azureKeyVaultResource.Name}.secrets.{SecretName}}}";

    ValueTask<string?> IValueProvider.GetValueAsync(CancellationToken cancellationToken)
    {
        if (azureKeyVaultResource.Secrets.TryGetValue(secretName, out var secretValue))
        {
            return new(secretValue);
        }

        throw new InvalidOperationException($"Secret '{secretName}' not found in Key Vault '{azureKeyVaultResource.Name}'.");
    }
}