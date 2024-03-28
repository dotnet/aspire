// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for configuring OpenTelemetry in projects using environment variables.
/// </summary>
public static class OtlpConfigurationExtensions
{
    /// <summary>
    /// Configures OpenTelemetry in projects using environment variables.
    /// </summary>
    /// <param name="resources">The set of resources in the model.</param>
    /// <param name="resource">The resource to add annotations to.</param>
    /// <param name="environment">The host environment to check if the application is running in development mode.</param>
    public static void AddOtlpEnvironment(IResourceCollection resources, IResource resource, IHostEnvironment environment)
    {
        // Configure OpenTelemetry in projects using environment variables.
        // https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/configuration/sdk-environment-variables.md

        resource.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
        {
            if (context.ExecutionContext.IsPublishMode)
            {
                // REVIEW:  Do we want to set references to an imaginary otlp provider as a requirement?
                return;
            }

            var otlpServer = (from r in resources
                              let otlpServerAnnotation = r.TryGetLastAnnotation<OtlpServerResourceAnnotation>(out var annotation) ? annotation : null
                              where otlpServerAnnotation is not null
                              select otlpServerAnnotation).FirstOrDefault();

            if (otlpServer is null)
            {
                return;
            }

            // Set the service name and instance id to the resource name and UID. Values are injected by DCP.
            context.EnvironmentVariables["OTEL_RESOURCE_ATTRIBUTES"] = "service.instance.id={{- .Name -}}";
            context.EnvironmentVariables["OTEL_SERVICE_NAME"] = "{{- index .Annotations \"otel-service-name\" -}}";

            if (otlpServer.GrpcEndpoint is not null)
            {
                context.EnvironmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = otlpServer.GrpcEndpoint;
            }
            else if (otlpServer.GrpcUrl is { } otlpGrpcUrl)
            {
                context.EnvironmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = otlpGrpcUrl;
            }

            if (otlpServer.ApiKey is { } otlpApiKey)
            {
                context.EnvironmentVariables["OTEL_EXPORTER_OTLP_HEADERS"] = $"x-otlp-api-key={otlpApiKey}";
            }

            // Set a small batch schedule delay in development.
            // This reduces the delay that OTLP exporter waits to sends telemetry and makes the dashboard telemetry pages responsive.
            if (environment.IsDevelopment())
            {
                var value = "1000"; // milliseconds
                context.EnvironmentVariables["OTEL_BLRP_SCHEDULE_DELAY"] = value;
                context.EnvironmentVariables["OTEL_BSP_SCHEDULE_DELAY"] = value;
                context.EnvironmentVariables["OTEL_METRIC_EXPORT_INTERVAL"] = value;
            }
        }));
    }

    /// <summary>
    /// Injects the appropriate environment variables to allow the resource to enable sending telemetry to the dashboard.
    /// 1. It sets the OTLP endpoint to the value of the DOTNET_DASHBOARD_OTLP_ENDPOINT_URL environment variable.
    /// 2. It sets the service name and instance id to the resource name and UID. Values are injected by the orchestrator.
    /// 3. It sets a small batch schedule delay in development. This reduces the delay that OTLP exporter waits to sends telemetry and makes the dashboard telemetry pages responsive.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithOtlpExporter<T>(this IResourceBuilder<T> builder) where T : IResourceWithEnvironment
    {
        AddOtlpEnvironment(builder.ApplicationBuilder.Resources, builder.Resource, builder.ApplicationBuilder.Environment);
        return builder;
    }
}
