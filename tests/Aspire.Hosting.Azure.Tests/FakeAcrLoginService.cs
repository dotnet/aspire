// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECONTAINERRUNTIME001

using Aspire.Hosting.Publishing;
using Azure.Core;

namespace Aspire.Hosting.Azure.Tests;

internal sealed class FakeAcrLoginService : IAcrLoginService
{
    private const string AcrUsername = "00000000-0000-0000-0000-000000000000";
    
    private readonly IContainerRuntime _containerRuntime;

    public bool WasLoginCalled { get; private set; }
    public string? LastRegistryEndpoint { get; private set; }
    public string? LastTenantId { get; private set; }

    public FakeAcrLoginService(IContainerRuntime containerRuntime)
    {
        _containerRuntime = containerRuntime ?? throw new ArgumentNullException(nameof(containerRuntime));
    }

    public async Task LoginAsync(
        string registryEndpoint,
        string tenantId,
        TokenCredential credential,
        CancellationToken cancellationToken = default)
    {
        WasLoginCalled = true;
        LastRegistryEndpoint = registryEndpoint;
        LastTenantId = tenantId;
        
        // Call the container runtime to match real implementation behavior
        // This allows tests to verify the container runtime was called
        await _containerRuntime.LoginToRegistryAsync(registryEndpoint, AcrUsername, "fake-refresh-token", cancellationToken);
    }
}
