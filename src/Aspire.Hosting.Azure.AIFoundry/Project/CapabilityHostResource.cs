// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning;
using Azure.Provisioning.CognitiveServices;

namespace Aspire.Hosting.Azure.AIFoundry;

/// <summary>
/// The Azure Cognitive Services project-level capability host resource.
/// </summary>
public class AzureCognitiveServicesProjectCapabilityHostResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure, AzureCognitiveServicesProjectResource parent) :
    AzureResourceManagerAspireResourceWithParent<CognitiveServicesProjectCapabilityHost, AzureCognitiveServicesProjectResource>(name, configureInfrastructure, parent)
{
    internal AzureCosmosDBResource? CosmosDB { get; set; }
    internal AzureKeyVaultResource? KeyVault { get; set; }
    internal AzureStorageResource? Storage { get; set; }
    internal AzureStorageResource? VirtualNetwork { get; set; }

    /// <inheritdoc/>
    public override CognitiveServicesProjectCapabilityHost FromExisting(string bicepIdentifier)
    {
        return CognitiveServicesProjectCapabilityHost.FromExisting(bicepIdentifier);
    }

    /// <inheritdoc/>
    public override void SetName(CognitiveServicesProjectCapabilityHost provisionableResource, BicepValue<string> name)
    {
        provisionableResource.Name = name;
    }
}
