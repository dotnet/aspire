// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for <see cref="IDistributedApplicationBuilder"/> to add and configure project resources.
/// </summary>
public static class ProjectResourceBuilderExtensions
{
    /// <summary>
    /// Adds a .NET project to the application model. By default, this will exist in a Projects namespace. e.g. Projects.MyProject.
    /// If the project is not in a Projects namespace, make sure a project reference is added from the AppHost project to the target project.
    /// </summary>
    /// <typeparam name="TProject">A type that represents the project reference.</typeparam>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used for service discovery when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ProjectResource> AddProject<TProject>(this IDistributedApplicationBuilder builder, string name) where TProject : IProjectMetadata, new()
    {
        var project = new ProjectResource(name);
        return builder.AddResource(project)
                      .WithAnnotation(new TProject())
                      .WithProjectDefaults(excludeLaunchProfile: false, launchProfileName: null);
    }

    /// <summary>
    /// Adds a .NET project to the application model. 
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used for service discovery when referenced in a dependency.</param>
    /// <param name="projectPath">The path to the project file.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ProjectResource> AddProject(this IDistributedApplicationBuilder builder, string name, string projectPath)
    {
        var project = new ProjectResource(name);

        projectPath = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, projectPath));

        return builder.AddResource(project)
                      .WithAnnotation(new ProjectMetadata(projectPath))
                      .WithProjectDefaults(excludeLaunchProfile: false, launchProfileName: null);
    }

    /// <summary>
    /// Adds a .NET project to the application model. By default, this will exist in a Projects namespace. e.g. Projects.MyProject.
    /// If the project is not in a Projects namespace, make sure a project reference is added from the AppHost project to the target project.
    /// </summary>
    /// <typeparam name="TProject">A type that represents the project reference.</typeparam>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used for service discovery when referenced in a dependency.</param>
    /// <param name="launchProfileName">The launch profile to use. If <c>null</c> then no launch profile will be used.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ProjectResource> AddProject<TProject>(this IDistributedApplicationBuilder builder, string name, string? launchProfileName) where TProject : IProjectMetadata, new()
    {
        var project = new ProjectResource(name);
        return builder.AddResource(project)
                      .WithAnnotation(new TProject())
                      .WithProjectDefaults(excludeLaunchProfile: launchProfileName is null, launchProfileName);
    }

    /// <summary>
    /// Adds a .NET project to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used for service discovery when referenced in a dependency.</param>
    /// <param name="projectPath">The path to the project file.</param>
    /// <param name="launchProfileName">The launch profile to use. If <c>null</c> then no launch profile will be used.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ProjectResource> AddProject(this IDistributedApplicationBuilder builder, string name, string projectPath, string? launchProfileName)
    {
        var project = new ProjectResource(name);

        projectPath = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, projectPath));

        return builder.AddResource(project)
                      .WithAnnotation(new ProjectMetadata(projectPath))
                      .WithProjectDefaults(excludeLaunchProfile: launchProfileName is null, launchProfileName);
    }

    private static IResourceBuilder<ProjectResource> WithProjectDefaults(this IResourceBuilder<ProjectResource> builder, bool excludeLaunchProfile, string? launchProfileName)
    {
        // We only want to turn these on for .NET projects, ConfigureOtlpEnvironment works for any resource type that
        // implements IDistributedApplicationResourceWithEnvironment.
        builder.WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES", "true");
        builder.WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES", "true");
        builder.WithOtlpExporter();
        builder.ConfigureConsoleLogs();

        if (excludeLaunchProfile)
        {
            builder.WithAnnotation(new ExcludeLaunchProfileAnnotation());
            return builder;
        }

        if (!string.IsNullOrEmpty(launchProfileName))
        {
            builder.WithAnnotation(new LaunchProfileAnnotation(launchProfileName));
        }

        var projectResource = builder.Resource;

        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            // Automatically add EndpointAnnotation to project resources based on ApplicationUrl set in the launch profile.
            var launchProfile = projectResource.GetEffectiveLaunchProfile(throwIfNotFound: true);
            if (launchProfile is null)
            {
                return builder;
            }

            var urlsFromApplicationUrl = launchProfile.ApplicationUrl?.Split(';') ?? [];
            foreach (var url in urlsFromApplicationUrl)
            {
                var uri = new Uri(url);

                builder.WithEndpoint(uri.Scheme, e =>
                {
                    e.Port = uri.Port;
                    e.UriScheme = uri.Scheme;
                },
                createIfNotExists: true);
            }
        }
        else
        {
            // If we aren't a web project we don't automatically add bindings.
            if (!IsWebProject(projectResource))
            {
                return builder;
            }

            var isHttp2ConfiguredInAppSettings = IsKestrelHttp2ConfigurationPresent(projectResource);

            if (!projectResource.Annotations.OfType<EndpointAnnotation>().Any(sb => sb.UriScheme == "http" || string.Equals(sb.Name, "http", StringComparisons.EndpointAnnotationName)))
            {
                builder.WithEndpoint("http", e =>
                {
                    e.UriScheme = "http";
                    e.Transport = isHttp2ConfiguredInAppSettings ? "http2" : e.Transport;
                },
                createIfNotExists: true);
            }

            if (!projectResource.Annotations.OfType<EndpointAnnotation>().Any(sb => sb.UriScheme == "https" || string.Equals(sb.Name, "https", StringComparisons.EndpointAnnotationName)))
            {
                builder.WithEndpoint("https", e =>
                {
                    e.UriScheme = "https";
                    e.Transport = isHttp2ConfiguredInAppSettings ? "http2" : e.Transport;
                },
                createIfNotExists: true);
            }
        }

        return builder;
    }

    /// <summary>
    /// Configures how many replicas of the project should be created for the project.
    /// </summary>
    /// <param name="builder">The project resource builder.</param>
    /// <param name="replicas">The number of replicas.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ProjectResource> WithReplicas(this IResourceBuilder<ProjectResource> builder, int replicas)
    {
        builder.WithAnnotation(new ReplicaAnnotation(replicas));
        return builder;
    }

    /// <summary>
    /// Configures how many replicas of the project should be created for the project.
    /// </summary>
    /// <param name="builder">The executable resource builder.</param>
    /// <param name="replicas">The number of replicas.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ExecutableResource> WithReplicas(this IResourceBuilder<ExecutableResource> builder, int replicas)
    {
        builder.WithAnnotation(new ReplicaAnnotation(replicas));
        return builder;
    }

    /// <summary>
    /// Configures which launch profile should be used when running the project.
    /// </summary>
    /// <param name="builder">The project resource builder.</param>
    /// <param name="launchProfileName">The name of the launch profile to use for execution.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    [Obsolete("This API is replaced by the AddProject overload that accepts a launchProfileName. Method will be removed by GA.")]
    public static IResourceBuilder<ProjectResource> WithLaunchProfile(this IResourceBuilder<ProjectResource> builder, string launchProfileName)
    {
        throw new InvalidOperationException("This API is replaced by the AddProject overload that accepts a launchProfileName. Method will be removed by GA.");
    }

    /// <summary>
    /// Configures the project to exclude launch profile settings when running.
    /// </summary>
    /// <param name="builder">The project resource builder.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    [Obsolete("This API is replaced by the AddProject overload that accepts a launchProfileName. Null means exclude launch profile. Method will be removed by GA.")]
    public static IResourceBuilder<ProjectResource> ExcludeLaunchProfile(this IResourceBuilder<ProjectResource> builder)
    {
        throw new InvalidOperationException("This API is replaced by the AddProject overload that accepts a launchProfileName. Null means exclude launch profile. Method will be removed by GA.");
    }

    private static bool IsKestrelHttp2ConfigurationPresent(ProjectResource projectResource)
    {
        var projectMetadata = projectResource.GetProjectMetadata();

        var projectDirectoryPath = Path.GetDirectoryName(projectMetadata.ProjectPath)!;
        var appSettingsPath = Path.Combine(projectDirectoryPath, "appsettings.json");
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        var appSettingsEnvironmentPath = Path.Combine(projectDirectoryPath, $"appsettings.{env}.json");

        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddJsonFile(appSettingsPath, optional: true);
        configBuilder.AddJsonFile(appSettingsEnvironmentPath, optional: true);
        var config = configBuilder.Build();
        var protocol = config["Kestrel:EndpointDefaults:Protocols"];
        return protocol == "Http2";
    }

    private static bool IsWebProject(ProjectResource projectResource)
    {
        var launchProfile = projectResource.GetEffectiveLaunchProfile(throwIfNotFound: true);
        return launchProfile?.ApplicationUrl != null;
    }
}
