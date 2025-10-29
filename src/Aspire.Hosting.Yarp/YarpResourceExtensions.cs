#pragma warning disable ASPIREDOCKERFILEBUILDER001

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Yarp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            yarpBuilder.WithEnvironment(ctx =>
            {
                var developerCertificateService = ctx.ExecutionContext.ServiceProvider.GetRequiredService<IDeveloperCertificateService>();
                if (!developerCertificateService.SupportsContainerTrust)
                {
                    // On systems without the ASP.NET DevCert updates introduced in .NET 10, YARP will not trust the cert used
                    // by Aspire otlp endpoint when running locally. The Aspire otlp endpoint uses the dev cert, and prior to
                    // .NET 10, it was only valid for localhost, but from the container perspective, the url will be something
                    // like https://docker.host.internal, so it will NOT be valid. This is not necessary when using the latest
                    // dev cert.
                    ctx.EnvironmentVariables["YARP_UNSAFE_OLTP_CERT_ACCEPT_ANY_SERVER_CERTIFICATE"] = "true";
                }
            });
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

    /// <summary>
    /// Enables static file serving in the YARP resource. Static files are served from the wwwroot folder.
    /// </summary>
    /// <param name="builder">The resource builder for YARP.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<YarpResource> WithStaticFiles(this IResourceBuilder<YarpResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEnvironment("YARP_ENABLE_STATIC_FILES", "true");
    }

    /// <summary>
    /// Enables static file serving. In run mode: bind mounts <paramref name="sourcePath"/> to /wwwroot.
    /// In publish mode: generates a Dockerfile whose build context is <paramref name="sourcePath"/> and
    /// copies its contents into /app/wwwroot baked into the image.
    /// </summary>
    /// <param name="builder">The resource builder for YARP.</param>
    /// <param name="sourcePath">The source path containing static files to serve.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<YarpResource> WithStaticFiles(this IResourceBuilder<YarpResource> builder, string sourcePath)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(sourcePath);

        builder.WithStaticFiles();

        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            builder.WithDockerfileFactory(sourcePath, ctx =>
            {
                var imageName = GetYarpImageName(ctx.Resource);

                return $"""
                FROM {imageName} AS yarp
                WORKDIR /app
                COPY . /app/wwwroot
                """;
            });
        }
        else
        {
            builder = builder.WithContainerFiles("/wwwroot", sourcePath);
        }

        return builder;
    }

    /// <summary>
    /// In publish mode, generates a Dockerfile that copies static files from the specified resource into /app/wwwroot.
    /// </summary>
    /// <param name="builder">The resource builder for YARP.</param>
    /// <param name="resourceWithFiles">The resource with container files.</param>
    /// <returns>The updated resource builder.</returns>
    public static IResourceBuilder<YarpResource> PublishWithStaticFiles(this IResourceBuilder<YarpResource> builder, IResourceBuilder<IResourceWithContainerFiles> resourceWithFiles)
    {
        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        // In publish mode, generate a Dockerfile that copies the container files into /app/wwwroot
        return builder
               .PublishWithContainerFiles(resourceWithFiles, "/app/wwwroot")
               .WithStaticFiles()
               .WithDockerfileBuilder(".", ctx =>
               {
                   var logger = ctx.Services.GetRequiredService<ILogger<YarpResource>>();
                   var source = resourceWithFiles.Resource;

                   var imageName = GetYarpImageName(ctx.Resource);
                   var stage = ctx.Builder.From(imageName);

                   if (!source.TryGetContainerImageName(out var sourceImageName))
                   {
                       logger.LogWarning("Cannot get container image name for source resource {SourceName}, skipping", source.Name);
                       return;
                   }

                   foreach (var containerFilesSource in resourceWithFiles.Resource.Annotations.OfType<ContainerFilesSourceAnnotation>())
                   {
                       stage.CopyFrom(sourceImageName, containerFilesSource.SourcePath, "/app/wwwroot");
                   }
               });
    }

    private static string GetYarpImageName(IResource resource)
    {
        if (!resource.TryGetContainerImageName(useBuiltImage: false, out var imageName) || string.IsNullOrEmpty(imageName))
        {
            imageName = $"{YarpContainerImageTags.Image}:{YarpContainerImageTags.Tag}";
        }

        return imageName;
    }
}
