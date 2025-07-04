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
                      .WithHttpEndpoint(name: "http", targetPort: Port)
                      .WithImage(YarpContainerImageTags.Image)
                      .WithImageRegistry(YarpContainerImageTags.Registry)
                      .WithImageTag(YarpContainerImageTags.Tag)
                      .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
                      .WithEntrypoint("dotnet")
                      .WithArgs("/app/yarp.dll")
                      .WithOtlpExporter();

        if (builder.ExecutionContext.IsRunMode)
        {
            // YARP will not trust the cert used by Aspire otlp endpoint when running locally
            // The Aspire otlp endpoint uses the dev cert, only valid for localhost, but from the container
            // perspective, the url will be something like https://docker.host.internal, so it will NOT be valid.
            yarpBuilder.WithEnvironment("YARP_UNSAFE_OLTP_CERT_ACCEPT_ANY_SERVER_CERTIFICATE", "true");
        }

        yarpBuilder.WithEnvironment(ctx =>
        {
            YarpEnvConfigGenerator.PopulateEnvVariables(ctx.EnvironmentVariables, yarpBuilder.Resource.Routes, yarpBuilder.Resource.Clusters);
        });

        return yarpBuilder;
    }

    /// <summary>
    /// Configure the YARP resource.
    /// </summary>
    /// <param name="builder">The YARP resource to configure.</param>
    /// <param name="configurationBuilder">The delegate to configure YARP.</param>
    public static IResourceBuilder<YarpResource> WithConfiguration(this IResourceBuilder<YarpResource> builder, Action<IYarpConfigurationBuilder> configurationBuilder)
    {
        var configBuilder = new YarpConfigurationBuilder(builder);
        configurationBuilder(configBuilder);
        return builder;
    }

    /// <summary>
    /// Configures the host port that the YARP resource is exposed on instead of using randomly assigned port.
    /// </summary>
    /// <param name="builder">The resource builder for YARP.</param>
    /// <param name="port">The port to bind on the host. If <see langword="null"/> is used random port will be assigned.</param>
    public static IResourceBuilder<YarpResource> WithHostPort(this IResourceBuilder<YarpResource> builder, int? port)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEndpoint("http", endpoint =>
        {
            endpoint.Port = port;
        });
    }
}
