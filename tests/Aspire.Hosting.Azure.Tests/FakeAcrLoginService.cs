// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;

namespace Aspire.Hosting.Azure.Tests;

internal sealed class FakeAcrLoginService : IAcrLoginService
{
    public bool WasLoginCalled { get; private set; }
    public string? LastRegistryEndpoint { get; private set; }
    public string? LastTenantId { get; private set; }

    public Task LoginAsync(
        string registryEndpoint,
        string? tenantId,
        TokenCredential credential,
        CancellationToken cancellationToken = default)
    {
        WasLoginCalled = true;
        LastRegistryEndpoint = registryEndpoint;
        LastTenantId = tenantId;
        return Task.CompletedTask;
    }
}
