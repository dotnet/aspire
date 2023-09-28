// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting.Otlp;

internal static class OtlpConfigurationExtensions
{
    private const string DashboardOtlpUrlVariableName = "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL";
    private const string DashboardOtlpUrlDefaultValue = "http://localhost:18889";

    public static IDistributedApplicationComponentBuilder<T> ConfigureOtlpEnvironment<T>(this IDistributedApplicationComponentBuilder<T> builder) where T : IDistributedApplicationComponent
    {
        // Configure OpenTelemetry in projects using environment variables.
        // https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/configuration/sdk-environment-variables.md

        return builder.WithAnnotation(
            new EnvironmentCallbackAnnotation((config) =>
            {
                config["OTEL_EXPORTER_OTLP_ENDPOINT"] = builder.ApplicationBuilder.Configuration[DashboardOtlpUrlVariableName] ?? DashboardOtlpUrlDefaultValue;

                // Set a small batch schedule delay in development.
                // This reduces the delay that OTLP exporter waits to sends telemetry and makes the dashboard telemetry pages responsive.
                if (builder.ApplicationBuilder.Environment.IsDevelopment())
                {
                    config["OTEL_BLRP_SCHEDULE_DELAY"] = "1000"; // milliseconds
                    config["OTEL_BSP_SCHEDULE_DELAY"] = "1000"; // milliseconds
                }
            }));
    }
}
