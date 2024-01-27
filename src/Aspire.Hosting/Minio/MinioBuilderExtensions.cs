// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Minio resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class MinioBuilderExtensions
{
    private const string RootUserEnvVarName = "MINIO_ROOT_USER";
    private const string RootPasswordEnvVarName = "MINIO_ROOT_PASSWORD";

    /// <summary>
    /// Adds a Minio container to the application model. The default image is "minio/minio" and the tag is "latest".
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="minioAdminPort">The host port for Minio Admin.</param>
    /// <param name="minioPort">The host port for Minio.</param>
    /// <param name="rootUser">The root user for the Minio server.</param>
    /// <param name="rootPassword">The password for the Minio root user.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{MinioContainerResource}"/>.</returns>
    public static IResourceBuilder<MinioContainerResource> AddMinioContainer(
        this IDistributedApplicationBuilder builder,
        string name,
        string rootUser,
        string rootPassword,
        int minioPort = 9000,
        int minioAdminPort = 9001)
    {
        var minioContainer = new MinioContainerResource(name, rootUser, rootPassword);

        return builder
            .AddResource(minioContainer)
            .WithManifestPublishingCallback(context => WriteMinioContainerToManifest(context, minioContainer))
            .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, port: minioPort, containerPort: 9000, name: "minio"))
            .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, port: minioAdminPort, containerPort: 9001, name: "minio"))
            .WithAnnotation(new ContainerImageAnnotation { Image = "minio/minio", Tag = "latest" })
            .WithEnvironment("MINIO_ADDRESS", ":9000")
            .WithEnvironment("MINIO_CONSOLE_ADDRESS", ":9001")
            .WithEnvironment(context =>
            {
                if (context.PublisherName == "manifest")
                {
                    context.EnvironmentVariables.Add(RootUserEnvVarName, $"{{{minioContainer.Name}.inputs.rootUser}}");
                    context.EnvironmentVariables.Add(RootPasswordEnvVarName, $"{{{minioContainer.Name}.inputs.rootPassword}}");
                }
                else
                {
                    context.EnvironmentVariables.Add(RootUserEnvVarName, minioContainer.RootUser);
                    context.EnvironmentVariables.Add(RootPasswordEnvVarName, minioContainer.RootPassword);
                }
            })
            .WithArgs("server", "/data");
    }

    private static void WriteMinioContainerToManifest(ManifestPublishingContext context, MinioContainerResource resource)
    {
        // Want to see if there is interest 
        context.WriteContainer(resource);
    }
}
