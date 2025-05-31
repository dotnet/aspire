// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Docker;

/// <summary>
/// Represents the infrastructure for Docker Compose within the Aspire Hosting environment.
/// Implements the <see cref="IDistributedApplicationLifecycleHook"/> interface to provide lifecycle hooks for distributed applications.
/// </summary>
internal sealed class DockerComposeInfrastructure(
    ILogger<DockerComposeInfrastructure> logger,
    DistributedApplicationExecutionContext executionContext) : IDistributedApplicationLifecycleHook
{
    public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        if (executionContext.IsRunMode)
        {
            return;
        }

        // Find Docker Compose environment resources
        var dockerComposeEnvironments = appModel.Resources.OfType<DockerComposeEnvironmentResource>().ToArray();

        if (dockerComposeEnvironments.Length > 1)
        {
            throw new NotSupportedException("Multiple Docker Compose environments are not supported.");
        }

        var environment = dockerComposeEnvironments.FirstOrDefault();

        if (environment == null)
        {
            return;
        }

        // Check if dashboard is enabled and create it if needed
        ContainerResource? dashboardResource = null;
        if (environment.DashboardEnabled)
        {
            dashboardResource = CreateDashboardResource(appModel);
        }

        var dockerComposeEnvironmentContext = new DockerComposeEnvironmentContext(environment, logger);

        foreach (var r in appModel.Resources)
        {
            if (r.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var lastAnnotation) && lastAnnotation == ManifestPublishingCallbackAnnotation.Ignore)
            {
                continue;
            }

            // Skip resources that are not containers or projects
            if (!r.IsContainer() && r is not ProjectResource)
            {
                continue;
            }

            // Configure OTLP for this resource if dashboard is enabled
            if (dashboardResource != null)
            {
                ConfigureOtlpForResource(r, dashboardResource);
            }

            // Create a Docker Compose compute resource for the resource
            var serviceResource = await dockerComposeEnvironmentContext.CreateDockerComposeServiceResourceAsync(r, executionContext, cancellationToken).ConfigureAwait(false);

            // Add deployment target annotation to the resource
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            r.Annotations.Add(new DeploymentTargetAnnotation(serviceResource)
            {
                ComputeEnvironment = environment
            });
#pragma warning restore ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        }
    }

    private static ContainerResource CreateDashboardResource(DistributedApplicationModel appModel)
    {
        // Create dashboard container resource
        var dashboardResource = new ContainerResource("aspire-dashboard");
        
        // Add container image annotation
        dashboardResource.Annotations.Add(new ContainerImageAnnotation { Image = "mcr.microsoft.com/dotnet/nightly/aspire-dashboard" });
        
        // Add endpoint annotations
        dashboardResource.Annotations.Add(new EndpointAnnotation(ProtocolType.Tcp, targetPort: 18888, name: "dashboard"));
        dashboardResource.Annotations.Add(new EndpointAnnotation(ProtocolType.Tcp, targetPort: 18889, name: "otlp"));

        // Add Docker Compose service customization for restart policy
        dashboardResource.Annotations.Add(new DockerComposeServiceCustomizationAnnotation((serviceResource, service) =>
        {
            service.Restart = "always";
        }));

        // Add the dashboard resource to the model
        appModel.Resources.Add(dashboardResource);
        
        return dashboardResource;
    }

    private static void ConfigureOtlpForResource(IResource resource, ContainerResource dashboardResource)
    {
        // Skip the dashboard itself
        if (resource == dashboardResource)
        {
            return;
        }

        // Only configure OTLP for resources that have the OtlpExporterAnnotation and implement IResourceWithEnvironment
        if (resource is IResourceWithEnvironment resourceWithEnv && resource.Annotations.OfType<OtlpExporterAnnotation>().Any())
        {
            // Configure OTLP environment variables
            resourceWithEnv.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
            {
                var otlpEndpoint = dashboardResource.GetEndpoint("otlp");
                context.EnvironmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = otlpEndpoint;
                context.EnvironmentVariables["OTEL_EXPORTER_OTLP_PROTOCOL"] = "grpc";
                context.EnvironmentVariables["OTEL_SERVICE_NAME"] = resource.Name;
                return Task.CompletedTask;
            }));
        }
    }
}
