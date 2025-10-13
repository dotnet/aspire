// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Azure.Core;
using Aspire.Hosting.Azure.Provisioning.Internal;

namespace Aspire.Hosting.Azure.Provisioning;

internal sealed record UserPrincipal(Guid Id, string Name);

internal sealed class ProvisioningContext(
    TokenCredential credential,
    IArmClient armClient,
    ISubscriptionResource subscription,
    IResourceGroupResource resourceGroup,
    ITenantResource tenant,
    AzureLocation location,
    UserPrincipal principal,
    JsonObject deploymentState,
    DistributedApplicationExecutionContext executionContext)
{
    // Lock object to protect concurrent access to DeploymentState from multiple provisioning tasks
    private readonly object _deploymentStateLock = new();

    public TokenCredential Credential => credential;
    public IArmClient ArmClient => armClient;
    public ISubscriptionResource Subscription => subscription;
    public ITenantResource Tenant => tenant;
    public IResourceGroupResource ResourceGroup => resourceGroup;
    public AzureLocation Location => location;
    public UserPrincipal Principal => principal;
    public JsonObject DeploymentState => deploymentState;
    public DistributedApplicationExecutionContext ExecutionContext => executionContext;

    /// <summary>
    /// Executes an action on the DeploymentState with thread-safe synchronization.
    /// Use this method to perform any read or write operations on the DeploymentState
    /// when multiple resources are being provisioned in parallel.
    /// </summary>
    public void WithDeploymentState(Action<JsonObject> action)
    {
        lock (_deploymentStateLock)
        {
            action(deploymentState);
        }
    }

    /// <summary>
    /// Executes a function on the DeploymentState with thread-safe synchronization.
    /// Use this method to perform any read or write operations on the DeploymentState
    /// when multiple resources are being provisioned in parallel.
    /// </summary>
    public T WithDeploymentState<T>(Func<JsonObject, T> func)
    {
        lock (_deploymentStateLock)
        {
            return func(deploymentState);
        }
    }
}
