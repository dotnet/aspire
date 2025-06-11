// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Security.KeyVault.Secrets;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Default implementation of <see cref="ISecretClientProvider"/>.
/// </summary>
internal sealed class DefaultSecretClientProvider(ITokenCredentialProvider tokenCredentialProvider) : ISecretClientProvider
{
    public SecretClient GetSecretClient(Uri vaultUri) =>
        new(vaultUri, tokenCredentialProvider.TokenCredential);
}
