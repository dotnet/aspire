// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp.Model;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding OTLP endpoint resources to the distributed application model.
/// </summary>
public static class OtlpEndpointResourceBuilderExtensions
{
    /// <summary>
    /// Adds an OTLP endpoint resource to the distributed application model.
    /// This represents an OpenTelemetry Protocol endpoint that can receive telemetry data.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the OTLP endpoint resource.</param>
    /// <param name="grpcEndpoint">Optional gRPC endpoint URL. If not specified, will use ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL from configuration.</param>
    /// <param name="httpEndpoint">Optional HTTP endpoint URL. If not specified, will use ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL from configuration.</param>
    /// <returns>A resource builder for the OTLP endpoint.</returns>
    /// <remarks>
    /// <para>
    /// This method creates an OTLP endpoint resource that can be referenced by other resources
    /// in the application model to configure telemetry export. The resource can represent the
    /// Aspire Dashboard's OTLP endpoint or any external OTLP collector.
    /// </para>
    /// <para>
    /// If no endpoints are specified, the resource will use the dashboard OTLP endpoints
    /// configured via ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL and ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL
    /// environment variables.
    /// </para>
    /// </remarks>
    /// <example>
    /// Add the default dashboard OTLP endpoint:
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// var otlp = builder.AddOtlpEndpoint("otlp");
    /// 
    /// var myService = builder.AddProject&lt;Projects.MyService&gt;("myservice")
    ///     .WithReference(otlp);
    /// </code>
    /// 
    /// Add a custom OTLP collector endpoint:
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// var otlp = builder.AddOtlpEndpoint("grafana-otlp", 
    ///     grpcEndpoint: "http://grafana:4317",
    ///     httpEndpoint: "http://grafana:4318");
    /// 
    /// var myService = builder.AddProject&lt;Projects.MyService&gt;("myservice")
    ///     .WithReference(otlp);
    /// </code>
    /// </example>
    public static IResourceBuilder<OtlpEndpointResource> AddOtlpEndpoint(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        string? grpcEndpoint = null,
        string? httpEndpoint = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var resource = new OtlpEndpointResource(name);

        // Resolve endpoints from configuration if not explicitly provided
        var configuration = builder.Configuration;
        var resolvedGrpcEndpoint = grpcEndpoint ?? configuration.GetString(KnownConfigNames.DashboardOtlpGrpcEndpointUrl, KnownConfigNames.Legacy.DashboardOtlpGrpcEndpointUrl);
        var resolvedHttpEndpoint = httpEndpoint ?? configuration.GetString(KnownConfigNames.DashboardOtlpHttpEndpointUrl, KnownConfigNames.Legacy.DashboardOtlpHttpEndpointUrl);

        // Determine the primary endpoint based on availability (prefer gRPC)
        string primaryScheme;
        int primaryPort;

        if (resolvedGrpcEndpoint is not null && Uri.TryCreate(resolvedGrpcEndpoint, UriKind.Absolute, out var grpcUri))
        {
            primaryScheme = grpcUri.Scheme;
            primaryPort = grpcUri.Port;
        }
        else if (resolvedHttpEndpoint is not null && Uri.TryCreate(resolvedHttpEndpoint, UriKind.Absolute, out var httpUri))
        {
            primaryScheme = httpUri.Scheme;
            primaryPort = httpUri.Port;
        }
        else
        {
            // Fallback to default
            primaryScheme = "http";
            primaryPort = 18889;
        }

        // Add primary endpoint annotation
        resource.Annotations.Add(new EndpointAnnotation(
            ProtocolType.Tcp,
            uriScheme: primaryScheme,
            name: "otlp",
            port: primaryPort,
            isProxied: false)
        {
            TargetHost = "localhost"
        });

        // Add gRPC endpoint if available
        if (resolvedGrpcEndpoint is not null && Uri.TryCreate(resolvedGrpcEndpoint, UriKind.Absolute, out var grpcEndpointUri))
        {
            resource.Annotations.Add(new EndpointAnnotation(
                ProtocolType.Tcp,
                uriScheme: grpcEndpointUri.Scheme,
                name: "otlp-grpc",
                port: grpcEndpointUri.Port,
                isProxied: false)
            {
                TargetHost = grpcEndpointUri.Host
            });
        }

        // Add HTTP endpoint if available
        if (resolvedHttpEndpoint is not null && Uri.TryCreate(resolvedHttpEndpoint, UriKind.Absolute, out var httpEndpointUri))
        {
            resource.Annotations.Add(new EndpointAnnotation(
                ProtocolType.Tcp,
                uriScheme: httpEndpointUri.Scheme,
                name: "otlp-http",
                port: httpEndpointUri.Port,
                isProxied: false)
            {
                TargetHost = httpEndpointUri.Host
            });
        }

        var resourceBuilder = builder.AddResource(resource)
            .ExcludeFromManifest(); // OTLP endpoints are infrastructure, not part of deployment manifest

        return resourceBuilder;
    }

    /// <summary>
    /// Adds a reference to an OTLP endpoint resource and configures the resource to send telemetry to it.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="otlpEndpoint">The OTLP endpoint resource to reference.</param>
    /// <param name="protocol">Optional OTLP protocol to use. If not specified, will prefer gRPC over HTTP.</param>
    /// <returns>The resource builder.</returns>
    /// <remarks>
    /// This method configures the resource to send OpenTelemetry data to the specified OTLP endpoint.
    /// It sets the appropriate environment variables (OTEL_EXPORTER_OTLP_ENDPOINT, OTEL_EXPORTER_OTLP_PROTOCOL, etc.)
    /// and applies development-optimized settings when running in development mode.
    /// </remarks>
    /// <example>
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// 
    /// var otlp = builder.AddOtlpEndpoint("otlp");
    /// 
    /// var myService = builder.AddProject&lt;Projects.MyService&gt;("myservice")
    ///     .WithReference(otlp);
    /// </code>
    /// </example>
    public static IResourceBuilder<T> WithReference<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<OtlpEndpointResource> otlpEndpoint,
        OtlpProtocol? protocol = null)
        where T : IResourceWithEnvironment
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(otlpEndpoint);

        // Add the OTLP exporter annotation to mark this resource as having OTLP configured
        builder.Resource.Annotations.Add(new OtlpExporterAnnotation { RequiredProtocol = protocol });

        // Configure environment variables for OTLP
        builder.WithEnvironment(context =>
        {
            // Determine which endpoint to use based on protocol preference
            EndpointReference endpointRef;
            string otlpProtocol;

            if (protocol == OtlpProtocol.Grpc)
            {
                endpointRef = otlpEndpoint.Resource.GrpcEndpoint;
                otlpProtocol = "grpc";
            }
            else if (protocol == OtlpProtocol.HttpProtobuf)
            {
                endpointRef = otlpEndpoint.Resource.HttpEndpoint;
                otlpProtocol = "http/protobuf";
            }
            else if (protocol == OtlpProtocol.HttpJson)
            {
                endpointRef = otlpEndpoint.Resource.HttpEndpoint;
                otlpProtocol = "http/json";
            }
            else
            {
                // Default: prefer primary endpoint
                endpointRef = otlpEndpoint.Resource.PrimaryEndpoint;
                otlpProtocol = "grpc"; // Default to gRPC
            }

            // Set the OTLP endpoint URL using the reference expression
            var urlExpression = endpointRef.Property(EndpointProperty.Url);
            context.EnvironmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = urlExpression;
            context.EnvironmentVariables["OTEL_EXPORTER_OTLP_PROTOCOL"] = otlpProtocol;

            // Set the service name and instance id (injected by DCP)
            context.EnvironmentVariables["OTEL_RESOURCE_ATTRIBUTES"] = "service.instance.id={{- index .Annotations \"" + CustomResource.OtelServiceInstanceIdAnnotation + "\" -}}";
            context.EnvironmentVariables["OTEL_SERVICE_NAME"] = "{{- index .Annotations \"" + CustomResource.OtelServiceNameAnnotation + "\" -}}";

            // Apply OTLP API key if configured
            if (builder.ApplicationBuilder.Configuration["AppHost:OtlpApiKey"] is { } otlpApiKey)
            {
                context.EnvironmentVariables["OTEL_EXPORTER_OTLP_HEADERS"] = $"x-otlp-api-key={otlpApiKey}";
            }

            // Configure OTLP to quickly provide all data with a small delay in development
            if (builder.ApplicationBuilder.Environment.IsDevelopment())
            {
                var value = "1000"; // milliseconds
                context.EnvironmentVariables["OTEL_BLRP_SCHEDULE_DELAY"] = value;
                context.EnvironmentVariables["OTEL_BSP_SCHEDULE_DELAY"] = value;
                context.EnvironmentVariables["OTEL_METRIC_EXPORT_INTERVAL"] = value;
                context.EnvironmentVariables["OTEL_TRACES_SAMPLER"] = "always_on";
                context.EnvironmentVariables["OTEL_METRICS_EXEMPLAR_FILTER"] = "trace_based";
                context.EnvironmentVariables["OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT"] = "true";
            }
        });

        return builder;
    }
}
