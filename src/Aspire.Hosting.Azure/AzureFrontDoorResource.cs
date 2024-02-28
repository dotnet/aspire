// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Front Door resource.
/// </summary>
/// <param name="name"></param>
public class AzureFrontDoorResource(string name) : AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.frontdoor.bicep")
{
    /// <summary>
    /// Gets the "endpointHostName" output reference from the bicep template for the Azure Front Door resource.
    /// </summary>
    public BicepOutputReference EndpointHostName => new("endpointHostName", this);
}
