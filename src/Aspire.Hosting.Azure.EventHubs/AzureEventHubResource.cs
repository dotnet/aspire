// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Event Hub resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="eventHubsNamespace">The <see cref="AzureEventHubsResource"/> namespace that the resource belongs to.</param>
public class AzureEventHubResource(string name, AzureEventHubsResource eventHubsNamespace) : Resource(name), 
    IResourceWithParent<AzureEventHubsResource>,
    IResourceWithConnectionString,
    IResourceWithAzureFunctionsConfig
{
    /// <summary>
    /// 
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        Parent.IsEmulator
            ? ReferenceExpression.Create($"Endpoint=sb://{Parent.EmulatorEndpoint.Property(EndpointProperty.Host)}:{Parent.EmulatorEndpoint.Property(EndpointProperty.Port)};SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;EntityPath={Name}")
            : ReferenceExpression.Create($"{Parent.EventHubsEndpoint};EntityPath={Name}");

    /// <summary>
    /// 
    /// </summary>
    public AzureEventHubsResource Parent => eventHubsNamespace;

    void IResourceWithAzureFunctionsConfig.ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName)
    {
        AzureEventHubsResource.ApplyAzureFunctionsConfigurationInternal(target, connectionName,
            eventHubsNamespace.IsEmulator,  ConnectionStringExpression, eventHubsNamespace.EventHubsEndpoint);
    }
}
