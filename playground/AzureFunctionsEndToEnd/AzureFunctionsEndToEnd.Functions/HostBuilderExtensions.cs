using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

public static class HostBuilderExtensions
{
    /// <remarks>
    /// The `AddServiceDefaults` method that Aspire provides out of the box is designed
    /// to target IHostApplicationBuilder. Function Apps still use HostBuilder so we implement
    /// a dupe of the `AddServiceDefaults` implementation here to get things working properly.
    /// Long-term, Functions apps should use `IHostApplicationBuilder`.
    /// </remarks>
    public static HostBuilder AddServiceDefaults(this HostBuilder builder)
    {
        builder.ConfigureLogging(x =>
        {
            x.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });
        });

        builder.ConfigureServices((context, services) =>
        {
            services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics.AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation();
                })
                .WithTracing(tracing =>
                {
                    tracing.AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation();
                });
        });

        builder.ConfigureServices(services =>
        {
            services.AddServiceDiscovery();

            services.ConfigureHttpClientDefaults(http =>
            {
                // Turn on resilience by default
                http.AddStandardResilienceHandler();

                // Turn on service discovery by default
                http.AddServiceDiscovery();
            });
        });

        return builder;
    }
}
