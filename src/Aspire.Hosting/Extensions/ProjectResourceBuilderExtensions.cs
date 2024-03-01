// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Properties;
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
                      .WithProjectDefaults();
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
                      .WithProjectDefaults();
    }

    private static IResourceBuilder<ProjectResource> WithProjectDefaults(this IResourceBuilder<ProjectResource> builder)
    {
        // We only want to turn these on for .NET projects, ConfigureOtlpEnvironment works for any resource type that
        // implements IDistributedApplicationResourceWithEnvironment.
        builder.WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES", "true");
        builder.WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES", "true");
        builder.WithOtlpExporter();
        builder.ConfigureConsoleLogs();

        var projectResource = builder.Resource;

        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            // Automatically add EndpointAnnotation to project resources based on ApplicationUrl set in the launch profile.
            var launchProfile = projectResource.GetEffectiveLaunchProfile();
            if (launchProfile is null)
            {
                return builder;
            }

            var urlsFromApplicationUrl = launchProfile.ApplicationUrl?.Split(';') ?? [];
            foreach (var url in urlsFromApplicationUrl)
            {
                var uri = new Uri(url);

                var endpointAnnotations = projectResource.Annotations.OfType<EndpointAnnotation>().Where(sb => string.Equals(sb.Name, uri.Scheme, StringComparisons.EndpointAnnotationName));
                if (endpointAnnotations.Any(sb => sb.IsProxied))
                {
                    // If someone uses WithEndpoint in the dev host to register a endpoint with the name
                    // http or https this exception will be thrown.
                    throw new DistributedApplicationException($"Endpoint with name '{uri.Scheme}' already exists.");
                }

                if (endpointAnnotations.Any())
                {
                    // We have a non-proxied endpoint with the same name as the 'url', don't add another endpoint for the same name
                    continue;
                }

                var generatedEndpointAnnotation = new EndpointAnnotation(
                    ProtocolType.Tcp,
                    uriScheme: uri.Scheme,
                    port: uri.Port,
                    source: "launchSettings"
                    );

                projectResource.Annotations.Add(generatedEndpointAnnotation);
            }
        }
        else
        {
            var isHttp2ConfiguredInAppSettings = IsKestrelHttp2ConfigurationPresent(projectResource);

            // If we aren't a web project we don't automatically add bindings.
            if (!IsWebProject(projectResource))
            {
                return builder;
            }

            if (!projectResource.Annotations.OfType<EndpointAnnotation>().Any(sb => sb.UriScheme == "http" || string.Equals(sb.Name, "http", StringComparisons.EndpointAnnotationName)))
            {
                var httpBinding = new EndpointAnnotation(
                    ProtocolType.Tcp,
                    uriScheme: "http",
                    source: "project"
                    );
                projectResource.Annotations.Add(httpBinding);
                httpBinding.Transport = isHttp2ConfiguredInAppSettings ? "http2" : httpBinding.Transport;
            }

            if (!projectResource.Annotations.OfType<EndpointAnnotation>().Any(sb => sb.UriScheme == "https" || string.Equals(sb.Name, "https", StringComparisons.EndpointAnnotationName)))
            {
                var httpsBinding = new EndpointAnnotation(
                    ProtocolType.Tcp,
                    uriScheme: "https",
                    source: "project"
                    );
                projectResource.Annotations.Add(httpsBinding);
                httpsBinding.Transport = isHttp2ConfiguredInAppSettings ? "http2" : httpsBinding.Transport;
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
    public static IResourceBuilder<ProjectResource> WithLaunchProfile(this IResourceBuilder<ProjectResource> builder, string launchProfileName)
    {
        ArgumentException.ThrowIfNullOrEmpty(launchProfileName);

        var launchSettings = builder.Resource.GetLaunchSettings();

        if (launchSettings == null)
        {
            throw new DistributedApplicationException(Resources.LaunchProfileIsSpecifiedButLaunchSettingsFileIsNotPresentExceptionMessage);
        }

        if (!launchSettings.Profiles.TryGetValue(launchProfileName, out _))
        {
            var message = string.Format(CultureInfo.InvariantCulture, Resources.LaunchSettingsFileDoesNotContainProfileExceptionMessage, launchProfileName);
            throw new DistributedApplicationException(message);
        }

        var launchProfileAnnotation = new LaunchProfileAnnotation(launchProfileName);
        return builder.WithAnnotation(launchProfileAnnotation);
    }

    /// <summary>
    /// Configures the project to exclude launch profile settings when running.
    /// </summary>
    /// <param name="builder">The project resource builder.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ProjectResource> ExcludeLaunchProfile(this IResourceBuilder<ProjectResource> builder)
    {
        for (var i = 0; i < builder.Resource.Annotations.Count; i++)
        {
            if (builder.Resource.Annotations[i] is EndpointAnnotation ea)
            {
                if (ea.Source == "launchSettings" || ea.Source == "project")
                {
                    builder.Resource.Annotations.RemoveAt(i);
                    i--;
                }
            }
        }

        builder.WithAnnotation(new ExcludeLaunchProfileAnnotation());
        return builder;
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
        var launchProfile = projectResource.GetEffectiveLaunchProfile();
        return launchProfile?.ApplicationUrl != null;
    }
}
