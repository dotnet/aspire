// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Python;

/// <summary>
/// Provides extension methods for adding Python Flask applications to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class FlaskProjectResourceExtensionBuilder
{
    /// <summary>
    /// The python script in the project is automatically instrumented with opentelemetry when the virtual environment
    /// contains the opentelemetry-instrument executable. You can get this by adding the opentelemtry-distro package
    /// to your python project. In addition to the opentelemetry-distro package, you need to add the opentelemetry-exporter-otlp,
    /// and the opentelemetry-instrumentation-flask packages to your project. This will allow for the traces, logs, and metrics
    /// to be exported to the Aspire observability platform. 
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resource to.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="projectDirectory">The path to the directory containing the python project files.</param>
    /// <param name="entrypoint">The name of the module that contains the Flask app variable. Alternatively you can use a
    /// factory method in the form module:create_app()</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<FlaskProjectResource> AddFlaskProjectWithVirtualEnvironment(
        this IDistributedApplicationBuilder builder,  string name, string projectDirectory, string entrypoint)
    {
        projectDirectory = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, projectDirectory));
        var virtualEnvironment = new VirtualEnvironment(projectDirectory);

        var instrumentationExecutable = virtualEnvironment.GetExecutable("opentelemetry-instrument");
        var flaskExecutable = virtualEnvironment.GetRequiredExecutable("flask");
        var projectExecutable = instrumentationExecutable ?? flaskExecutable;
        var projectResource = new FlaskProjectResource(name, projectExecutable, projectDirectory);

        var resourceBuilder = builder.AddResource(projectResource).WithArgs(context =>
        {
            // If the project is to be automatically instrumented, add the instrumentation executable arguments first.

            if (!string.IsNullOrEmpty(instrumentationExecutable))
            {
                context.Args.Add("--traces_exporter");
                context.Args.Add("otlp");

                context.Args.Add("--logs_exporter");
                context.Args.Add("console,otlp");

                context.Args.Add("--metrics_exporter");
                context.Args.Add("otlp");

                // Add the python executable as the next argument so we can run the project.
                context.Args.Add(flaskExecutable!);
            }

            context.Args.Add("--app");
            context.Args.Add(entrypoint);
            context.Args.Add("run");
        });

        // Expose the port on the FLASK_RUN_PORT to allow the Flask app to bind to the correct port.
        resourceBuilder.WithHttpEndpoint(targetPort: 5000, name: "http", env: "FLASK_RUN_PORT");

        if (!string.IsNullOrEmpty(instrumentationExecutable))
        {
            resourceBuilder.WithOtlpExporter();

            // Make sure to attach the logging instrumentation setting, so we can capture logs.
            // Without this you'll need to configure logging yourself. Which is kind of a pain.
            resourceBuilder.WithEnvironment("OTEL_PYTHON_LOGGING_AUTO_INSTRUMENTATION_ENABLED", "true");
        }

        // Python projects need their own Dockerfile, we can't provide it through the hosting package.
        // Maybe in the future we can add a way to provide a Dockerfile template.
        resourceBuilder.WithManifestPublishingCallback(projectResource.WriteDockerFileManifestAsync);

        return resourceBuilder;
    }
}
