// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

public static class OtlpConfigurationExtensions
{
    private const string DashboardOtlpUrlVariableName = "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL";
    private const string DashboardOtlpUrlDefaultValue = "http://localhost:18889";

    public static void AddOtlpEnvironment(IDistributedApplicationResource resource, IConfiguration configuration, IHostEnvironment environment)
    {
        // Configure OpenTelemetry in projects using environment variables.
        // https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/configuration/sdk-environment-variables.md

        resource.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
        {
            if (context.PublisherName == "manifest")
            {
                // REVIEW:  Do we want to set references to an imaginary otlp provider as a requirement?
                return;
            }

            context.EnvironmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = configuration[DashboardOtlpUrlVariableName] ?? DashboardOtlpUrlDefaultValue;

            // Set the service name and instance id to the resource name and UID. Values are injected by DCP.
            context.EnvironmentVariables["OTEL_RESOURCE_ATTRIBUTES"] = "service.instance.id={{- .UID -}}";
            context.EnvironmentVariables["OTEL_SERVICE_NAME"] = "{{- .Name -}}";

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

    public static IDistributedApplicationResourceBuilder<T> ConfigureOtlpEnvironment<T>(this IDistributedApplicationResourceBuilder<T> builder) where T : IDistributedApplicationResourceWithEnvironment
    {
        AddOtlpEnvironment(builder.Resource, builder.ApplicationBuilder.Configuration, builder.ApplicationBuilder.Environment);
        return builder;
    }
}
