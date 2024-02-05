// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

public class AzureBicepServiceBusResource(string name) :
    AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.servicebus.bicep"),
    IResourceWithConnectionString
{
    public string? GetConnectionString()
    {
        return Outputs["serviceBusEndpoint"];
    }
}

public static class AzureBicepServiceBusExtensions
{
    public static IResourceBuilder<AzureBicepServiceBusResource> AddBicepServiceBus(this IDistributedApplicationBuilder builder, string name, string[]? queueNames = null, string[]? topicNames = null)
    {
        var resource = new AzureBicepServiceBusResource(name)
        {
            ConnectionStringTemplate = $"{{{name}.outputs.serviceBusEndpoint}}"
        };

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
