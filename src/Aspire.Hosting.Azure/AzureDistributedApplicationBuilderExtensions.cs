// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

public static class AzureDistributedApplicationBuilderExtensions
{
    /// <summary>
    /// Adds an Azure Service Bus component to the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The builder to add the component to.</param>
    /// <param name="name">The name of the component.</param>
    /// <param name="serviceBusNamespace">
    /// The optional Service Bus namespace to use for this component. If not specified, uses the 'Aspire.Azure.Messaging.ServiceBus:Namespace' configuration value
    /// of <paramref name="builder"/>.Configuration.
    /// </param>
    public static IDistributedApplicationComponentBuilder<ServiceBusComponent> AddAzureServiceBus(this IDistributedApplicationBuilder builder, string name, string? serviceBusNamespace = null)
    {
        var componentBuilder = builder.AddComponent(name, new ServiceBusComponent());
        componentBuilder.WithAnnotation(new ServiceBusAnnotation(serviceBusNamespace));
        return componentBuilder;
    }

    /// <summary>
    /// Adds the Azure ServiceBus represented by <paramref name="serviceBusBuilder"/> as a service that can be used by the Project represented by <paramref name="projectBuilder"/>.
    /// </summary>
    public static IDistributedApplicationComponentBuilder<ProjectComponent> WithAzureServiceBus(this IDistributedApplicationComponentBuilder<ProjectComponent> projectBuilder, IDistributedApplicationComponentBuilder<ServiceBusComponent> serviceBusBuilder)
    {
        return projectBuilder.WithEnvironment((config) =>
        {
            if (!serviceBusBuilder.Component.TryGetLastAnnotation<ServiceBusAnnotation>(out var sbAnnotation))
            {
                throw new DistributedApplicationException("Service bus component must have a ServiceBusAnnotation.");
            }

            var namespaceName = sbAnnotation.Namespace ?? projectBuilder.ApplicationBuilder.Configuration["Aspire:Azure:Messaging:ServiceBus:Namespace"];

            if (namespaceName is not null)
            {
                config.Add("Aspire__Azure__Messaging__ServiceBus__Namespace", namespaceName);
            }
        });
    }
}
