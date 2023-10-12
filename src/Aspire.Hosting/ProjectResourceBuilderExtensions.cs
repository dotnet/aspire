// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Otlp;
using Aspire.Hosting.Properties;

namespace Aspire.Hosting;

public static class ProjectResourceBuilderExtensions
{
    public static IDistributedApplicationResourceBuilder<ProjectResource> AddProject<TProject>(this IDistributedApplicationBuilder builder, string name) where TProject : IServiceMetadata, new()
    {
        var project = new ProjectResource(name);
        var projectBuilder = builder.AddResource(project);
        projectBuilder.ConfigureOtlpEnvironment();
        projectBuilder.ConfigureConsoleLogs();
        var serviceMetadata = new TProject();
        projectBuilder.WithAnnotation(serviceMetadata);
        return projectBuilder;
    }

    public static IDistributedApplicationResourceBuilder<ProjectResource> WithReplicas(this IDistributedApplicationResourceBuilder<ProjectResource> builder, int replicas)
    {
        builder.WithAnnotation(new ReplicaAnnotation(replicas));
        return builder;
    }
    public static IDistributedApplicationResourceBuilder<ExecutableResource> WithReplicas(this IDistributedApplicationResourceBuilder<ExecutableResource> builder, int replicas)
    {
        builder.WithAnnotation(new ReplicaAnnotation(replicas));
        return builder;
    }

    public static IDistributedApplicationResourceBuilder<ProjectResource> WithLaunchProfile(this IDistributedApplicationResourceBuilder<ProjectResource> builder, string launchProfileName)
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
