// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREAZURE003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting;

/// <summary>
/// Detects SQL servers with private endpoints and stores metadata annotations
/// so that AddRoleAssignments can create deployment script infrastructure inline.
/// </summary>
internal sealed class AzureSqlDeploymentScriptPreparer : IDistributedApplicationEventingSubscriber
{
    public Task SubscribeAsync(IDistributedApplicationEventing eventing, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        if (executionContext.IsPublishMode)
        {
            eventing.Subscribe<BeforeStartEvent>(OnBeforeStartAsync);
        }
        return Task.CompletedTask;
    }

    private Task OnBeforeStartAsync(BeforeStartEvent @event, CancellationToken cancellationToken)
    {
        var appModel = @event.Model;

        var sqlServers = appModel.Resources.OfType<AzureSqlServerResource>()
            .Where(sql => sql.HasAnnotationOfType<PrivateEndpointTargetAnnotation>())
            .ToList();

        foreach (var sql in sqlServers)
        {
            var hasExplicitSubnet = sql.TryGetLastAnnotation<AdminDeploymentScriptSubnetAnnotation>(out _);
            var hasExplicitStorage = sql.TryGetLastAnnotation<AdminDeploymentScriptStorageAnnotation>(out _);

            // Find the private endpoint targeting this SQL server to get the VirtualNetwork
            var pe = appModel.Resources.OfType<AzurePrivateEndpointResource>()
                .FirstOrDefault(p => ReferenceEquals(p.Target, sql));

            if (pe is null)
            {
                continue;
            }

            var peSubnet = pe.Subnet;
            var vnet = peSubnet.Parent;

            // Always include PeSubnet — it's needed for the files PE on both auto and explicit storage.
            var autoConfig = new AutoDeploymentScriptConfigAnnotation
            {
                PeSubnet = peSubnet,
                AutoCreateStorage = !hasExplicitStorage
            };

            // Only auto-allocate subnet if user didn't provide one
            if (!hasExplicitSubnet)
            {
                var existingSubnets = appModel.Resources.OfType<AzureSubnetResource>()
                    .Where(s => ReferenceEquals(s.Parent, vnet));

                var aciSubnetCidr = SubnetAddressAllocator.AllocateDeploymentScriptSubnet(vnet, existingSubnets);
                autoConfig = autoConfig with { VNet = vnet, AciSubnetCidr = aciSubnetCidr };
            }

            sql.Annotations.Add(autoConfig);
        }

        return Task.CompletedTask;
    }
}
