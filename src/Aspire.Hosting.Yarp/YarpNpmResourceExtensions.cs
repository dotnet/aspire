// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Yarp;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding YARP npm resources to the application model.
/// </summary>
[Experimental("ASPIREHOSTING001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public static class YarpNpmResourceExtensions
{
    /// <summary>
    /// Adds a YARP container configured to host static assets from a Node.js build.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="workingDirectory">The working directory containing the Node.js project.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{YarpNpmResource}"/>.</returns>
    /// <remarks>
    /// This method creates a YARP container that serves static files built from a Node.js project.
    /// The default configuration uses npm as the package manager, runs "npm install" and "npm run build",
    /// and expects the output in a "dist" folder. Use the fluent configuration methods to customize these defaults.
    /// </remarks>
    [Experimental("ASPIREHOSTING001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<YarpNpmResource> AddYarpNpmApp(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        string workingDirectory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(workingDirectory);

        var fullyQualifiedWorkingDirectory = Path.GetFullPath(workingDirectory, builder.AppHostDirectory)
                                                 .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var resource = new YarpNpmResource(name);
        var options = new NodeStaticBuildOptions();
        
        // Set up initial container configuration (similar to AddYarp)
        var resourceBuilder = builder.AddResource(resource)
            .WithHttpEndpoint(name: "http", targetPort: 5000)
            .WithImage("placeholder") // Temporary image, will be replaced by WithDockerfile
            .WithImageRegistry(null)
            .WithImageTag("latest")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
            .WithEntrypoint("dotnet")
            .WithArgs("/app/yarp.dll")
            .WithOtlpExporter();

        if (builder.ExecutionContext.IsRunMode)
        {
            resourceBuilder.WithEnvironment("YARP_UNSAFE_OLTP_CERT_ACCEPT_ANY_SERVER_CERTIFICATE", "true");
        }

        // Add the static build options annotation
        resourceBuilder.WithAnnotation(new NodeStaticBuildOptionsAnnotation(options));
        
        // Enable static file serving
        resourceBuilder.WithStaticFiles();
        
        // Add the Dockerfile factory (this will replace the placeholder image)
        resourceBuilder.WithDockerfile(fullyQualifiedWorkingDirectory, context =>
        {
            var yarpResource = (YarpNpmResource)context.Resource;
            var optionsAnnotation = yarpResource.Annotations.OfType<NodeStaticBuildOptionsAnnotation>().Single();
            var buildOptions = optionsAnnotation.Options;

            var dockerfile = GenerateDockerfile(buildOptions);
            return Task.FromResult(dockerfile);
        });

        return resourceBuilder;
    }

    /// <summary>
    /// Configures the package manager to use for the Node.js build.
    /// </summary>
    /// <param name="builder">The resource builder for YARP npm resource.</param>
    /// <param name="packageManager">The package manager (e.g., "npm", "pnpm", "yarn").</param>
    /// <returns>The <see cref="IResourceBuilder{YarpNpmResource}"/>.</returns>
    [Experimental("ASPIREHOSTING001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<YarpNpmResource> WithPackageManager(
        this IResourceBuilder<YarpNpmResource> builder,
        string packageManager)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(packageManager);

        var options = GetOrCreateOptions(builder);
        options.PackageManager = packageManager;
        return builder;
    }

    /// <summary>
    /// Configures the install command for the Node.js build.
    /// </summary>
    /// <param name="builder">The resource builder for YARP npm resource.</param>
    /// <param name="command">The install command (e.g., "install", "ci", "install --frozen-lockfile").</param>
    /// <returns>The <see cref="IResourceBuilder{YarpNpmResource}"/>.</returns>
    [Experimental("ASPIREHOSTING001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<YarpNpmResource> WithInstallCommand(
        this IResourceBuilder<YarpNpmResource> builder,
        string command)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(command);

        var options = GetOrCreateOptions(builder);
        options.InstallCommand = command;
        return builder;
    }

    /// <summary>
    /// Configures the build command for the Node.js build.
    /// </summary>
    /// <param name="builder">The resource builder for YARP npm resource.</param>
    /// <param name="command">The build command (e.g., "run build", "build").</param>
    /// <returns>The <see cref="IResourceBuilder{YarpNpmResource}"/>.</returns>
    [Experimental("ASPIREHOSTING001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<YarpNpmResource> WithBuildCommand(
        this IResourceBuilder<YarpNpmResource> builder,
        string command)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(command);

        var options = GetOrCreateOptions(builder);
        options.BuildCommand = command;
        return builder;
    }

    /// <summary>
    /// Configures the output directory containing the built static assets.
    /// </summary>
    /// <param name="builder">The resource builder for YARP npm resource.</param>
    /// <param name="outputDir">The output directory path (relative to the working directory).</param>
    /// <returns>The <see cref="IResourceBuilder{YarpNpmResource}"/>.</returns>
    [Experimental("ASPIREHOSTING001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<YarpNpmResource> WithOutputDir(
        this IResourceBuilder<YarpNpmResource> builder,
        string outputDir)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(outputDir);

        var options = GetOrCreateOptions(builder);
        options.OutputDir = outputDir;
        return builder;
    }

    /// <summary>
    /// Configures the Node.js version to use for the build.
    /// </summary>
    /// <param name="builder">The resource builder for YARP npm resource.</param>
    /// <param name="version">The Node.js version (e.g., "22", "20", "18").</param>
    /// <returns>The <see cref="IResourceBuilder{YarpNpmResource}"/>.</returns>
    [Experimental("ASPIREHOSTING001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<YarpNpmResource> WithNodeVersion(
        this IResourceBuilder<YarpNpmResource> builder,
        string version)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(version);

        var options = GetOrCreateOptions(builder);
        options.NodeVersion = version;
        return builder;
    }

    private static NodeStaticBuildOptions GetOrCreateOptions(IResourceBuilder<YarpNpmResource> builder)
    {
        var annotation = builder.Resource.Annotations.OfType<NodeStaticBuildOptionsAnnotation>().SingleOrDefault();
        if (annotation is not null)
        {
            return annotation.Options;
        }

        var options = new NodeStaticBuildOptions();
        builder.WithAnnotation(new NodeStaticBuildOptionsAnnotation(options));
        return options;
    }

    private static string GenerateDockerfile(NodeStaticBuildOptions options)
    {
        var sb = new StringBuilder();
        
        // Build stage
        sb.AppendLine(CultureInfo.InvariantCulture, $"FROM node:{options.NodeVersion} AS build");
        sb.AppendLine("WORKDIR /src");
        sb.AppendLine();
        sb.AppendLine("# Copy package files");
        sb.AppendLine("COPY package*.json ./");
        
        // Add yarn.lock or pnpm-lock.yaml if using yarn or pnpm
        if (options.PackageManager == "yarn")
        {
            sb.AppendLine("COPY yarn.lock ./");
        }
        else if (options.PackageManager == "pnpm")
        {
            sb.AppendLine("COPY pnpm-lock.yaml ./");
        }
        
        sb.AppendLine();
        sb.AppendLine("# Install dependencies");
        sb.AppendLine(CultureInfo.InvariantCulture, $"RUN {options.PackageManager} {options.InstallCommand}");
        sb.AppendLine();
        sb.AppendLine("# Copy source files");
        sb.AppendLine("COPY . .");
        sb.AppendLine();
        sb.AppendLine("# Build the application");
        sb.AppendLine(CultureInfo.InvariantCulture, $"RUN {options.PackageManager} {options.BuildCommand}");
        sb.AppendLine();
        
        // Runtime stage - copy to YARP container
        sb.AppendLine("FROM mcr.microsoft.com/dotnet/aspnet:9.0");
        sb.AppendLine("WORKDIR /app");
        sb.AppendLine();
        sb.AppendLine("# Copy the YARP binaries from the base YARP image");
        sb.AppendLine(CultureInfo.InvariantCulture, $"COPY --from={YarpContainerImageTags.Registry}/{YarpContainerImageTags.Image}:{YarpContainerImageTags.Tag} /app /app");
        sb.AppendLine();
        sb.AppendLine("# Copy built static assets");
        sb.AppendLine(CultureInfo.InvariantCulture, $"COPY --from=build /src/{options.OutputDir} /wwwroot");
        sb.AppendLine();
        sb.AppendLine("ENTRYPOINT [\"dotnet\", \"/app/yarp.dll\"]");

        return sb.ToString();
    }
}
