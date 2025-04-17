// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Yarp;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding YARP resources to the application model.
/// </summary>
public static class YarpServiceExtensions
{
    /// <summary>
    /// Adds a YARP container to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<YarpResource> AddYarp(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name)
    {
        var resource = new YarpResource(name);

        var yarpBuilder = builder.AddResource(resource)
                      .WithHttpEndpoint(targetPort: YarpContainerImageTags.Port)
                      .WithImage(YarpContainerImageTags.Image)
                      .WithImageRegistry(YarpContainerImageTags.Registry)
                      .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
                      .WithOtlpExporter();

        if (builder.Environment.IsDevelopment())
        {
            // YARP will not trust the cert used by aspire otlp endpoint when running locally
            yarpBuilder.WithEnvironment("YARP_UNSAFE_OLTP_CERT_ACCEPT_ANY_SERVER_CERTIFICATE", "true");
        }

        // Map the configuration file
        yarpBuilder.ApplicationBuilder.Eventing.Subscribe<AfterEndpointsAllocatedEvent>((e, ct) =>
        {
            if (yarpBuilder.Resource.ConfigFilePath != null)
            {
                yarpBuilder.WithBindMount(yarpBuilder.Resource.ConfigFilePath, YarpContainerImageTags.ConfigFilePath, isReadOnly: true);
            }
            else
            {
                // TODO: build dynamically the config file if none provided.
                throw new DistributedApplicationException($"No configuration provided for YARP instance \"{yarpBuilder.Resource.Name}\"");
            }
            return Task.CompletedTask;
        });

        return yarpBuilder;
    }

    /// <summary>
    /// Set explicitly the config file to use for YARP.
    /// </summary>
    /// <param name="builder">The YARP resource to configure.</param>
    /// <param name="configFilePath">The path to the YARP config file.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<YarpResource> WithConfigFile(this IResourceBuilder<YarpResource> builder, string configFilePath)
    {
        builder.Resource.ConfigFilePath = configFilePath;
        return builder;
    }
}
