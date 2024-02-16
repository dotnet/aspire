// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Service Bus resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AzureBicepServiceBusResource(string name) :
    AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.servicebus.bicep"),
    IResourceWithConnectionString
{
    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Service Bus endpoint.
    /// </summary>
    public string ConnectionStringExpression => $"{{{Name}.outputs.serviceBusEndpoint}}";
    /// <summary>
    /// Gets the connection string for the Azure Service Bus endpoint.
    /// </summary>
    /// <returns>The connection string for the Azure Service Bus endpoint.</returns>
    public string? GetConnectionString()
    {
        return Outputs["serviceBusEndpoint"];
    }
}

/// <summary>
/// Provides extension methods for adding the Azure Service Bus resources to the application model.
/// </summary>
public static class AzureBicepServiceBusExtensions
{
    /// <summary>
    /// Adds an Azure Service Bus resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="queueNames">A list of queue names associated with this service bus resource.</param>
    /// <param name="topicNames">A list of topic names associated with this service bus resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureBicepServiceBusResource> AddBicepAzureServiceBus(this IDistributedApplicationBuilder builder, string name, string[]? queueNames = null, string[]? topicNames = null)
    {
        var resource = new AzureBicepServiceBusResource(name);
        // TODO: Change topics and queues to child resources

        return builder.AddResource(resource)
                      .WithParameter("serviceBusNamespaceName", resource.CreateBicepResourceName())
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                      .WithParameter("topics", topicNames ?? [])
                      .WithParameter("queues", queueNames ?? [])
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
