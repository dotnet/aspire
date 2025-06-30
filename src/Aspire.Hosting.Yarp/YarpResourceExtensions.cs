// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Yarp;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding YARP resources to the application model.
/// </summary>
public static class YarpResourceExtensions
{
    private const int Port = 5000;

    private const string ConfigDirectory = "/etc";

    private const string ConfigFileName = "yarp.config";

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
                      .WithHttpEndpoint(targetPort: Port)
                      .WithImage(YarpContainerImageTags.Image)
                      .WithImageRegistry(YarpContainerImageTags.Registry)
                      .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
                      .WithOtlpExporter();

        if (builder.ExecutionContext.IsRunMode)
        {
            // YARP will not trust the cert used by Aspire otlp endpoint when running locally
            // The Aspire otlp endpoint uses the dev cert, only valid for localhost, but from the container
            // perspective, the url will be something like https://docker.host.internal, so it will NOT be valid.
            yarpBuilder.WithEnvironment("YARP_UNSAFE_OLTP_CERT_ACCEPT_ANY_SERVER_CERTIFICATE", "true");
        }

        // Map the configuration file
        yarpBuilder.WithContainerFiles(ConfigDirectory, async (context, ct) =>
        {
            // Call all the config delegates
            var configBuilder = new YarpConfigurationBuilder(yarpBuilder);
            foreach (var configurator in yarpBuilder.Resource.ConfigurationBuilderDelegates)
            {
                configurator(configBuilder);
            }
            // Add all routes and cluster to the json config generator
            foreach (var route in configBuilder.Routes)
            {
                yarpBuilder.Resource.JsonConfigGenerator.AddRoute(route.RouteConfig);
            }
            foreach (var destination in configBuilder.Clusters)
            {
                yarpBuilder.Resource.JsonConfigGenerator.AddCluster(destination.ClusterConfig);
            }
            // Generate the json content
            var contents = await yarpBuilder.Resource.JsonConfigGenerator.Build(ct).ConfigureAwait(false);

            var configFile = new ContainerFile
            {
                Name = ConfigFileName,
                Contents = contents
            };

            return [configFile];
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
        builder.Resource.JsonConfigGenerator.WithConfigFile(configFilePath);
        return builder;
    }

    /// <summary>
    /// Configure the YARP resource.
    /// </summary>
    /// <param name="builder">The YARP resource to configure.</param>
    /// <param name="configurationBuilder">The delegate to configure YARP.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    internal static IResourceBuilder<YarpResource> WithConfiguration(this IResourceBuilder<YarpResource> builder, Action<IYarpJsonConfigGeneratorBuilder> configurationBuilder)
    {
        configurationBuilder(builder.Resource.JsonConfigGenerator);
        return builder;
    }

    /// <summary>
    /// Configure the YARP resource.
    /// </summary>
    /// <param name="builder">The YARP resource to configure.</param>
    /// <param name="configurationBuilder">The delegate to configure YARP.</param>
    public static IResourceBuilder<YarpResource> WithConfiguration(this IResourceBuilder<YarpResource> builder, Action<IYarpConfigurationBuilder> configurationBuilder)
    {
        builder.Resource.ConfigurationBuilderDelegates.Add(configurationBuilder);
        return builder;
    }
}
