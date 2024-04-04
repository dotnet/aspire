// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.SignalR;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure SignalR resources to the application model.
/// </summary>
public static class AzureSignalRExtensions
{
    /// <summary>
    /// Adds an Azure SignalR resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureSignalRResource> AddAzureSignalR(this IDistributedApplicationBuilder builder, string name)
    {
#pragma warning disable AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return builder.AddAzureSignalR(name, null);
#pragma warning restore AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }

    /// <summary>
    /// Adds an Azure SignalR resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="configureResource">Callback to configure the underlying <see cref="global::Azure.Provisioning.SignalR.SignalRService"/> resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    [Experimental("AZPROVISION001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<AzureSignalRResource> AddAzureSignalR(this IDistributedApplicationBuilder builder, string name, Action<IResourceBuilder<AzureSignalRResource>, ResourceModuleConstruct, SignalRService>? configureResource)
    {
        builder.AddAzureProvisioning();

        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var service = new SignalRService(construct, name: name);
            service.AssignProperty(x => x.Kind, "'SignalR'");
            service.AddOutput("hostName", x => x.HostName);

            service.Properties.Tags["aspire-resource-name"] = construct.Resource.Name;

            var appServerRole = service.AssignRole(RoleDefinition.SignalRAppServer);
            appServerRole.AssignProperty(x => x.PrincipalId, construct.PrincipalIdParameter);
            appServerRole.AssignProperty(x => x.PrincipalType, construct.PrincipalTypeParameter);

            var resource = (AzureSignalRResource)construct.Resource;
            var resourceBuilder = builder.CreateResourceBuilder(resource);
            configureResource?.Invoke(resourceBuilder, construct, service);
        };

        var resource = new AzureSignalRResource(name, configureConstruct);
        return builder.AddResource(resource)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
