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
    private const string AspNetCoreForwaredHeadersEnabledVariableName = "ASPNETCORE_FORWARDEDHEADERS_ENABLED";

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
        // .NET SDK has experimental support for retries. Enable with env var.
        // https://github.com/open-telemetry/opentelemetry-dotnet/pull/5495
        // Remove once retry feature in opentelemetry-dotnet is enabled by default.
        builder.WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY", "in_memory");

        builder.WithOtlpExporter();
        builder.ConfigureConsoleLogs();
        builder.SetAspNetCoreUrls();

        var projectResource = builder.Resource;

        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            builder.WithEnvironment(context =>
            {
                // If we have any endpoints & the forwarded headers wasn't disabled then add it
                if (projectResource.GetEndpoints().Any() && !projectResource.Annotations.OfType<DisableForwardedHeadersAnnotation>().Any())
                {
                    context.EnvironmentVariables[AspNetCoreForwaredHeadersEnabledVariableName] = "true";
                }
            });
        }

        if (excludeLaunchProfile)
        {
            builder.WithAnnotation(new ExcludeLaunchProfileAnnotation());
            return builder;
        }

        if (!string.IsNullOrEmpty(launchProfileName))
        {
            builder.WithAnnotation(new LaunchProfileAnnotation(launchProfileName));
        }

        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            // Process the launch profile and turn it into environment variables and endpoints.
            var launchProfile = projectResource.GetEffectiveLaunchProfile(throwIfNotFound: true);
            if (launchProfile is null)
            {
                return builder;
            }

            var urlsFromApplicationUrl = launchProfile.ApplicationUrl?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? [];
            foreach (var url in urlsFromApplicationUrl)
            {
                var uri = new Uri(url);

                builder.WithEndpoint(uri.Scheme, e =>
                {
                    e.Port = uri.Port;
                    e.UriScheme = uri.Scheme;
                    e.FromLaunchProfile = true;
                },
                createIfNotExists: true);
            }

            builder.WithEnvironment(context =>
            {
                // Populate DOTNET_LAUNCH_PROFILE environment variable for consistency with "dotnet run" and "dotnet watch".
                context.EnvironmentVariables.TryAdd("DOTNET_LAUNCH_PROFILE", launchProfileName!);

                foreach (var envVar in launchProfile.EnvironmentVariables)
                {
                    var value = Environment.ExpandEnvironmentVariables(envVar.Value);
                    context.EnvironmentVariables.TryAdd(envVar.Key, value);
                }
            });

            // NOTE: the launch profile command line arguments will be processed by ApplicationExecutor.PrepareProjectExecutables() (either by the IDE or manually passed to run)
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
    /// Configures the project to disable forwarded headers when being published.
    /// </summary>
    /// <param name="builder">The project resource builder.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ProjectResource> DisableForwardedHeaders(this IResourceBuilder<ProjectResource> builder)
    {
        builder.WithAnnotation<DisableForwardedHeadersAnnotation>(ResourceAnnotationMutationBehavior.Replace);
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
        var launchProfile = projectResource.GetEffectiveLaunchProfile(throwIfNotFound: true);
        return launchProfile?.ApplicationUrl != null;
    }

    private static void SetAspNetCoreUrls(this IResourceBuilder<ProjectResource> builder)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return;
        }

        builder.WithEnvironment(context =>
        {
            if (context.EnvironmentVariables.ContainsKey("ASPNETCORE_URLS"))
            {
                // If the user has already set ASPNETCORE_URLS, we don't want to override it.
                return;
            }

            var aspnetCoreUrls = new ReferenceExpressionBuilder();

            var processedHttpsPort = false;
            var first = true;

            static bool IsValidAspNetCoreUrl(EndpointAnnotation e) =>
                e.UriScheme is "http" or "https" && e.TargetPortEnvironmentVariable is null;

            // Turn http and https endpoints into a single ASPNETCORE_URLS environment variable.
            foreach (var e in builder.Resource.GetEndpoints().Where(e => IsValidAspNetCoreUrl(e.EndpointAnnotation)))
            {
                if (!first)
                {
                    aspnetCoreUrls.AppendLiteral(";");
                }

                if (!processedHttpsPort && e.EndpointAnnotation.UriScheme == "https")
                {
                    // Add the environment variable for the HTTPS port if we have an HTTPS service. This will make sure the
                    // HTTPS redirection middleware avoids redirecting to the internal port.
                    context.EnvironmentVariables["ASPNETCORE_HTTPS_PORT"] = e.Property(EndpointProperty.Port);

                    processedHttpsPort = true;
                }

                aspnetCoreUrls.Append($"{e.Property(EndpointProperty.Scheme)}://localhost:{e.Property(EndpointProperty.TargetPort)}");
                first = false;
            }

            if (!aspnetCoreUrls.IsEmpty)
            {
                // Combine into a single expression
                context.EnvironmentVariables["ASPNETCORE_URLS"] = aspnetCoreUrls.Build();
            }
        });
    }
}
