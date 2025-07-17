// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for configuring OpenTelemetry in projects using environment variables.
/// </summary>
public static class OtlpConfigurationExtensions
{
    private const string DashboardOtlpUrlDefaultValue = "http://localhost:18889";

    /// <summary>
    /// Configures OpenTelemetry in projects using environment variables.
    /// </summary>
    /// <param name="resource">The resource to add annotations to.</param>
    /// <param name="configuration">The configuration to use for the OTLP exporter endpoint URL.</param>
    /// <param name="environment">The host environment to check if the application is running in development mode.</param>
    public static void AddOtlpEnvironment(IResource resource, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        // Add annotation to mark this resource as having OTLP exporter configured
        resource.Annotations.Add(new OtlpExporterAnnotation());

        RegisterOtlpEnvironment(resource, configuration, environment);
    }

    /// <summary>
    /// Configures OpenTelemetry in projects using environment variables.
    /// </summary>
    /// <param name="resource">The resource to add annotations to.</param>
    /// <param name="configuration">The configuration to use for the OTLP exporter endpoint URL.</param>
    /// <param name="environment">The host environment to check if the application is running in development mode.</param>
    /// <param name="protocol">The protocol to use for the OTLP exporter. If not set, it will try gRPC then Http.</param>
    public static void AddOtlpEnvironment(IResource resource, IConfiguration configuration, IHostEnvironment environment, OtlpProtocol protocol)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        // Add annotation to mark this resource as having OTLP exporter configured
        resource.Annotations.Add(new OtlpExporterAnnotation { RequiredProtocol = protocol });

        RegisterOtlpEnvironment(resource, configuration, environment);
    }

    private static void RegisterOtlpEnvironment(IResource resource, IConfiguration configuration, IHostEnvironment environment)
    {
        // Configure OpenTelemetry in projects using environment variables.
        // https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/configuration/sdk-environment-variables.md

        resource.Annotations.Add(new EnvironmentCallbackAnnotation(async context =>
        {
            if (context.ExecutionContext.IsPublishMode)
            {
                // REVIEW:  Do we want to set references to an imaginary otlp provider as a requirement?
                return;
            }

            if (!resource.TryGetLastAnnotation<OtlpExporterAnnotation>(out var otlpExporterAnnotation))
            {
                return;
            }

            SetOtel(context, configuration, otlpExporterAnnotation);

            // Set the service name and instance id to the resource name and UID. Values are injected by DCP.
            var dcpDependencyCheckService = context.ExecutionContext.ServiceProvider.GetRequiredService<IDcpDependencyCheckService>();
            var dcpInfo = await dcpDependencyCheckService.GetDcpInfoAsync(cancellationToken: context.CancellationToken).ConfigureAwait(false);
            context.EnvironmentVariables["OTEL_RESOURCE_ATTRIBUTES"] = "service.instance.id={{- index .Annotations \"" + CustomResource.OtelServiceInstanceIdAnnotation + "\" -}}";
            context.EnvironmentVariables["OTEL_SERVICE_NAME"] = "{{- index .Annotations \"" + CustomResource.OtelServiceNameAnnotation + "\" -}}";

            if (configuration["AppHost:OtlpApiKey"] is { } otlpApiKey)
            {
                context.EnvironmentVariables["OTEL_EXPORTER_OTLP_HEADERS"] = $"x-otlp-api-key={otlpApiKey}";
            }

            // Configure OTLP to quickly provide all data with a small delay in development.
            if (environment.IsDevelopment())
            {
                // Set a small batch schedule delay in development.
                // This reduces the delay that OTLP exporter waits to sends telemetry and makes the dashboard telemetry pages responsive.
                var value = "1000"; // milliseconds
                context.EnvironmentVariables["OTEL_BLRP_SCHEDULE_DELAY"] = value;
                context.EnvironmentVariables["OTEL_BSP_SCHEDULE_DELAY"] = value;
                context.EnvironmentVariables["OTEL_METRIC_EXPORT_INTERVAL"] = value;

                // Configure trace sampler to send all traces to the dashboard.
                context.EnvironmentVariables["OTEL_TRACES_SAMPLER"] = "always_on";
                // Configure metrics to include exemplars.
                context.EnvironmentVariables["OTEL_METRICS_EXEMPLAR_FILTER"] = "trace_based";
            }
        }));

        static void SetOtel(EnvironmentCallbackContext context, IConfiguration configuration, OtlpExporterAnnotation otlpExporterAnnotation)
        {
            var dashboardOtlpGrpcUrl = configuration.GetString(KnownConfigNames.DashboardOtlpGrpcEndpointUrl, KnownConfigNames.Legacy.DashboardOtlpGrpcEndpointUrl);
            var dashboardOtlpHttpUrl = configuration.GetString(KnownConfigNames.DashboardOtlpHttpEndpointUrl, KnownConfigNames.Legacy.DashboardOtlpHttpEndpointUrl);

            // Check if a specific protocol is required by the annotation
            if (otlpExporterAnnotation.RequiredProtocol is OtlpProtocol.Grpc)
            {
                SetOtelEndpointAndProtocol(context.EnvironmentVariables, dashboardOtlpGrpcUrl ?? DashboardOtlpUrlDefaultValue, "grpc");
            }
            else if (otlpExporterAnnotation.RequiredProtocol is OtlpProtocol.HttpProtobuf)
            {
                SetOtelEndpointAndProtocol(context.EnvironmentVariables, dashboardOtlpHttpUrl ?? throw new InvalidOperationException("OtlpExporter is configured to require http/protobuf, but no endpoint was configured for ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL"), "http/protobuf");
            }
            else
            {
                // No specific protocol required, use the existing preference logic
                // The dashboard can support OTLP/gRPC and OTLP/HTTP endpoints at the same time, but it can
                // only tell resources about one of the endpoints via environment variables.
                // If both OTLP/gRPC and OTLP/HTTP are available then prefer gRPC.
                if (dashboardOtlpGrpcUrl is not null)
                {
                    SetOtelEndpointAndProtocol(context.EnvironmentVariables, dashboardOtlpGrpcUrl, "grpc");
                }
                else if (dashboardOtlpHttpUrl is not null)
                {
                    SetOtelEndpointAndProtocol(context.EnvironmentVariables, dashboardOtlpHttpUrl, "http/protobuf");
                }
                else
                {
                    // No endpoints provided to host. Use default value for URL.
                    SetOtelEndpointAndProtocol(context.EnvironmentVariables, DashboardOtlpUrlDefaultValue, "grpc");
                }
            }
        }

        static void SetOtelEndpointAndProtocol(Dictionary<string, object> environmentVariables, string url, string protocol)
        {
            environmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = new HostUrl(url);
            environmentVariables["OTEL_EXPORTER_OTLP_PROTOCOL"] = protocol;
        }
    }

    /// <summary>
    /// Injects the appropriate environment variables to allow the resource to enable sending telemetry to the dashboard.
    /// 1. It sets the OTLP endpoint to the value of the ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL environment variable.
    /// 2. It sets the service name and instance id to the resource name and UID. Values are injected by the orchestrator.
    /// 3. It sets a small batch schedule delay in development. This reduces the delay that OTLP exporter waits to sends telemetry and makes the dashboard telemetry pages responsive.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithOtlpExporter<T>(this IResourceBuilder<T> builder) where T : IResourceWithEnvironment
    {
        ArgumentNullException.ThrowIfNull(builder);

        AddOtlpEnvironment(builder.Resource, builder.ApplicationBuilder.Configuration, builder.ApplicationBuilder.Environment);

        return builder;
    }

    /// <summary>
    /// Injects the appropriate environment variables to allow the resource to enable sending telemetry to the dashboard.
    /// 1. It sets the OTLP endpoint to the value of the ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL environment variable.
    /// 2. It sets the service name and instance id to the resource name and UID. Values are injected by the orchestrator.
    /// 3. It sets a small batch schedule delay in development. This reduces the delay that OTLP exporter waits to sends telemetry and makes the dashboard telemetry pages responsive.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="protocol">The protocol to use for the OTLP exporter. If not set, it will try gRPC then Http.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithOtlpExporter<T>(this IResourceBuilder<T> builder, OtlpProtocol protocol) where T : IResourceWithEnvironment
    {
        ArgumentNullException.ThrowIfNull(builder);

        AddOtlpEnvironment(builder.Resource, builder.ApplicationBuilder.Configuration, builder.ApplicationBuilder.Environment, protocol);

        return builder;
    }
}
