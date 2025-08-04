// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREAZURE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.Azure.Provisioning.Internal;

namespace Aspire.Hosting.Azure;

internal sealed class AzureDeployingContext(
    IProvisioningContextProvider provisioningContextProvider,
    IUserSecretsManager userSecretsManager)
{
    public async Task DeployModelAsync(CancellationToken cancellationToken = default)
    {
        var userSecrets = await userSecretsManager.LoadUserSecretsAsync(cancellationToken).ConfigureAwait(false);
        await provisioningContextProvider.CreateProvisioningContextAsync(userSecrets, cancellationToken).ConfigureAwait(false);
    }
}
