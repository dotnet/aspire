// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Otlp;
using Aspire.Hosting.Properties;

namespace Aspire.Hosting;

public static class ProjectComponentBuilderExtensions
{
    public static IDistributedApplicationComponentBuilder<ProjectComponent> AddProject<TProject>(this IDistributedApplicationBuilder builder, string? name = null) where TProject : IServiceMetadata, new()
    {
        var project = new ProjectComponent(name ?? typeof(TProject).Name.ToLowerInvariant());
        var projectBuilder = builder.AddComponent(project);
        projectBuilder.ConfigureOtlpEnvironment();
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

    public static IDistributedApplicationComponentBuilder<T> WithServiceBinding<T>(this IDistributedApplicationComponentBuilder<T> builder, int? hostPort = null, string? scheme = null, string? name = null) where T : IDistributedApplicationComponent
    {
        if (builder.Component.Annotations.OfType<ServiceBindingAnnotation>().Any(sb => sb.Name == name))
        {
            throw new DistributedApplicationException($"Service binding with name '{name}' already exists");
        }

        var annotation = new ServiceBindingAnnotation(ProtocolType.Tcp, scheme, name, hostPort);
        return builder.WithAnnotation(annotation);
    }
}
