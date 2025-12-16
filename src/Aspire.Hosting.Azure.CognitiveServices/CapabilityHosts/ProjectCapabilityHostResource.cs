// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.CognitiveServices;
using Azure.Provisioning;
using Azure.Provisioning.CognitiveServices;

/// <summary>
/// The Azure Cognitive Services account-level capability host resource. This becomes the default settings for all projects that don't have their own capability host defined.
/// </summary>
public class AzureCognitiveServicesCapabilityHostResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure, AzureCognitiveServicesAccountResource parent) :
    AzureResourceManagerAspireResourceWithParent<CognitiveServicesCapabilityHost, AzureCognitiveServicesAccountResource, CognitiveServicesAccount>(name, configureInfrastructure, parent)
{
    /// <inheritdoc/>
    public override CognitiveServicesCapabilityHost FromExisting(string bicepIdentifier)
    {
        return CognitiveServicesCapabilityHost.FromExisting(bicepIdentifier);
    }

    /// <inheritdoc/>
    public override void SetName(CognitiveServicesCapabilityHost provisionableResource, BicepValue<string> name)
    {
        provisionableResource.Name = name;
    }
}

/// <summary>
/// The Azure Cognitive Services project-level capability host resource.
/// </summary>
public class AzureCognitiveServicesProjectCapabilityHostResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure, AzureCognitiveServicesProjectResource parent) :
    AzureResourceManagerAspireResourceWithParent<CognitiveServicesProjectCapabilityHost, AzureCognitiveServicesProjectResource, CognitiveServicesProject>(name, configureInfrastructure, parent)
{
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
