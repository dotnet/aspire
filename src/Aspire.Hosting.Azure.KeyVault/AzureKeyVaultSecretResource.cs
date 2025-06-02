// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Key Vault Secret.
/// Initializes a new instance of the <see cref="AzureKeyVaultSecretResource"/> class.
/// </summary>
/// <remarks>
/// Use <see cref="AzureProvisioningResourceExtensions.ConfigureInfrastructure{T}(ApplicationModel.IResourceBuilder{T}, Action{AzureResourceInfrastructure})"/> to configure specific <see cref="Azure.Provisioning"/> properties.
/// </remarks>
public class AzureKeyVaultSecretResource(string name, string secretName, AzureKeyVaultResource parent, IManifestExpressionProvider value)
    : Resource(name), IResourceWithParent<AzureKeyVaultResource>, IAzureKeyVaultSecretReference
{
    /// <summary>
    /// Gets or sets the secret name.
    /// </summary>
    public string SecretName { get; set; } = ThrowIfNullOrEmpty(secretName);

    /// <summary>
    /// Gets the parent Azure Key Vault resource.
    /// </summary>
    public AzureKeyVaultResource Parent { get; } = parent ?? throw new ArgumentNullException(nameof(parent));

    /// <summary>
    /// Gets the value provider for the secret.
    /// </summary>
    public IManifestExpressionProvider Value { get; } = value ?? throw new ArgumentNullException(nameof(value));

    /// <summary>
    /// Gets the Azure Key Vault resource that contains this secret.
    /// </summary>
    IAzureKeyVaultResource IAzureKeyVaultSecretReference.Resource => Parent;

    /// <summary>
    /// Gets the expression for the secret value in the manifest.
    /// </summary>
    string IManifestExpressionProvider.ValueExpression => $"{{{Parent.Name}.secrets.{SecretName}}}";

    /// <summary>
    /// Gets the secret value asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The secret value.</returns>
    async ValueTask<string?> IValueProvider.GetValueAsync(CancellationToken cancellationToken)
    {
        if (Parent.SecretResolver is { } secretResolver)
        {
            return await secretResolver(this, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException($"Secret '{SecretName}' not found in Key Vault '{Parent.Name}'.");
    }

    private static string ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(argument, paramName);
        return argument;
    }
}