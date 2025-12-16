// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning;
using Azure.Provisioning.CognitiveServices;

namespace Aspire.Hosting.Azure.CognitiveServices;

/// <summary>
/// Represents a model deployment within an Azure Cognitive Services account.
/// </summary>
public class AzureCognitiveServicesAccountDeploymentResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure, AzureCognitiveServicesAccountResource parent) :
    AzureResourceManagerAspireResourceWithParent<CognitiveServicesAccountDeployment, AzureCognitiveServicesAccountResource, CognitiveServicesAccount>(name, configureInfrastructure, parent)
{
    /// <inheritdoc/>
    public override CognitiveServicesAccountDeployment FromExisting(string bicepIdentifier)
    {
        return CognitiveServicesAccountDeployment.FromExisting(bicepIdentifier);
    }

    /// <inheritdoc/>
    public override void SetName(CognitiveServicesAccountDeployment provisionableResource, BicepValue<string> name)
    {
        provisionableResource.Name = name;
    }
}
