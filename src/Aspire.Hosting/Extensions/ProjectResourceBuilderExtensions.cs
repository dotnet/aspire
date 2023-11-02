// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Properties;

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
    /// <returns>A reference to the <see cref="IResourceBuilder{ProjectResource}"/>.</returns>
    public static IResourceBuilder<ProjectResource> AddProject<TProject>(this IDistributedApplicationBuilder builder, string name) where TProject : IServiceMetadata, new()
    {
        var project = new ProjectResource(name);
        var projectBuilder = builder.AddResource(project);
        // We only want to turn these on for .NET projects, ConfigureOtlpEnvironment works for any resource type that
        // implements IDistributedApplicationResourceWithEnvironment.
        projectBuilder.WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES", "true");
        projectBuilder.WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES", "true");
        projectBuilder.WithOtlpExporter();
        projectBuilder.ConfigureConsoleLogs();
        var serviceMetadata = new TProject();
        projectBuilder.WithAnnotation(serviceMetadata);
        return projectBuilder;
    }

    /// <summary>
    /// Configures how many replicas of the project should be created for the project.
    /// </summary>
    /// <param name="builder">The project resource builder.</param>
    /// <param name="replicas">The number of replicas.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{ProjectResource}"/>.</returns>
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
    /// <returns>A reference to the <see cref="IResourceBuilder{ExecutableResource}"/>.</returns>
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
    /// <returns>A reference to the <see cref="IResourceBuilder{ProjectResource}"/>.</returns>
    public static IResourceBuilder<ProjectResource> WithLaunchProfile(this IResourceBuilder<ProjectResource> builder, string launchProfileName)
    {
        ArgumentException.ThrowIfNullOrEmpty(launchProfileName);

        var launchSettings = builder.Resource.GetLaunchSettings();

        if (launchSettings == null)
        {
            throw new DistributedApplicationException(Resources.LaunchProfileIsSpecifiedButLaunchSettingsFileIsNotPresentExceptionMessage);
        }

        if (!launchSettings.Profiles.TryGetValue(launchProfileName, out var launchProfile))
        {
            var message = string.Format(CultureInfo.InvariantCulture, Resources.LaunchSettingsFileDoesNotContainProfileExceptionMessage, launchProfileName);
            throw new DistributedApplicationException(message);
        }

        var launchProfileAnnotation = new LaunchProfileAnnotation(launchProfileName, launchProfile);
        return builder.WithAnnotation(launchProfileAnnotation);
    }
}
