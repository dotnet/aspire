// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Otlp;
using Aspire.Hosting.Properties;

namespace Aspire.Hosting;

public static class ProjectComponentBuilderExtensions
{
    public static IDistributedApplicationComponentBuilder<ProjectComponent> AddProject<TProject>(this IDistributedApplicationBuilder builder, string name) where TProject : IServiceMetadata, new()
    {
        var project = new ProjectComponent(name);
        var projectBuilder = builder.AddComponent(project);
        projectBuilder.ConfigureOtlpEnvironment();
        projectBuilder.ConfigureConsoleLogs();
        var serviceMetadata = new TProject();
        projectBuilder.WithAnnotation(serviceMetadata);
        return projectBuilder;
    }

    public static IDistributedApplicationComponentBuilder<ProjectComponent> WithReplicas(this IDistributedApplicationComponentBuilder<ProjectComponent> builder, int replicas)
    {
        builder.WithAnnotation(new ReplicaAnnotation(replicas));
        return builder;
    }
    public static IDistributedApplicationComponentBuilder<ExecutableComponent> WithReplicas(this IDistributedApplicationComponentBuilder<ExecutableComponent> builder, int replicas)
    {
        builder.WithAnnotation(new ReplicaAnnotation(replicas));
        return builder;
    }

    public static IDistributedApplicationComponentBuilder<ProjectComponent> WithLaunchProfile(this IDistributedApplicationComponentBuilder<ProjectComponent> builder, string launchProfileName)
    {
        ArgumentException.ThrowIfNullOrEmpty(launchProfileName);

        var launchSettings = builder.Component.GetLaunchSettings();

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
