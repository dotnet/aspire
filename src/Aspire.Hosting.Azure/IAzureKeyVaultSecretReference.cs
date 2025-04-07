// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents a reference to a secret in an Azure Key Vault resource.
/// </summary>
public interface IAzureKeyVaultSecretReference : IValueProvider, IManifestExpressionProvider
{
    /// <summary>
    /// Gets the name of the secret.
    /// </summary>
    string SecretName { get; }

    /// <summary>
    /// Gets the Azure Key Vault resource.
    /// </summary>
    IAzureKeyVaultResource Resource { get; }
}
