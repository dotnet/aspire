// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents a reference to a secret in an Azure Key Vault resource.
/// </summary>
/// <param name="secretName">The name of the secret.</param>
/// <param name="bicepOutputReference">The Bicep output reference.</param>
internal sealed class AzureKeyVaultSecretReference(string secretName, BicepOutputReference bicepOutputReference) : IKeyVaultSecretReference, IValueProvider, IManifestExpressionProvider
{
    /// <summary>
    /// Gets the name of the secret.
    /// </summary>
    public string SecretName => secretName;

    /// <summary>
    /// Gets the Azure Key Vault resource.
    /// </summary>
    public IKeyVaultResource Resource => (IKeyVaultResource)bicepOutputReference.Resource;

    string IManifestExpressionProvider.ValueExpression => bicepOutputReference.ValueExpression;

    async ValueTask<string?> IValueProvider.GetValueAsync(CancellationToken cancellationToken)
    {
        if (Resource.SecretResolver is null)
        {
            throw new InvalidOperationException($"The secret resolver is not set for the Azure Key Vault resource '{Resource.Name}'.");
        }

        return await Resource.SecretResolver(this, cancellationToken).ConfigureAwait(false);
    }
}
