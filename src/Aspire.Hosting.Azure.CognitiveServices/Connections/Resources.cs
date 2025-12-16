// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning;
using Azure.Provisioning.CognitiveServices;

namespace Aspire.Hosting.Azure.CognitiveServices;

/// <summary>
/// The Azure Cognitive Services connection resource scoped to a project.
/// </summary>
public class AzureCognitiveServicesProjectConnectionResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure, AzureCognitiveServicesProjectResource parent) :
    AzureResourceManagerAspireResourceWithParent<CognitiveServicesProjectConnection, AzureCognitiveServicesProjectResource, CognitiveServicesProject>(name, configureInfrastructure, parent)
{
    /// <inheritdoc/>
    public override CognitiveServicesProjectConnection FromExisting(string bicepIdentifier)
    {
        return CognitiveServicesProjectConnection.FromExisting(bicepIdentifier);
    }

    /// <inheritdoc/>

    public override void SetName(CognitiveServicesProjectConnection provisionableResource, BicepValue<string> name)
    {
        provisionableResource.Name = name;
    }
}
