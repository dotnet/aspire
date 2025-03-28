// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Security.KeyVault.Keys;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Azure.Security.KeyVault.HealthChecks;

internal class AzureKeyVaultKeysHealthCheck(KeyClient client, AzureSecurityKeyVaultSettings settings) : IHealthCheck
{
    internal KeyClient KeyClient => client;
    internal AzureSecurityKeyVaultSettings Settings => settings;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
