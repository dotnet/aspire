// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Python;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Python applications to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class PythonProjectResourceBuilderExtensions
{
    /// <summary>
    /// Adds a python application with a virtual environment to the application model.
    /// The Python project should have a virtual environment set up in the project directory under the name ".venv".
    /// <para>
    /// The python script in the project is automatically instrumented with opentelemetry when the virtual environment
    /// contains the opentelemetry-instrument executable. You can get this by adding the opentelemtry-distro package
    /// to your python project. In addition to the opentelemetry-distro package, you need to add the opentelemetry-exporter-otlp
    /// for the traces, logs, and metrics to be exported to the Aspire observability platform.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resource to.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="projectDirectory">The path to the directory containing the python project files.</param>
    /// <param name="scriptPath">The path to the script relative to the project directory to run.</param>
    /// <param name="scriptArgs">The arguments for the script.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<PythonProjectResource> AddPythonProjectWithVirtualEnvironment(
        this IDistributedApplicationBuilder builder, string name, string projectDirectory, string scriptPath, params string[] scriptArgs)
    {
        projectDirectory = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, projectDirectory));
        var virtualEnvironment = new VirtualEnvironment(projectDirectory);

        var instrumentationExecutable = virtualEnvironment.GetExecutable("opentelemetry-instrument");
        var pythonExecutable = virtualEnvironment.GetRequiredExecutable("python");

        var projectExecutable = instrumentationExecutable ?? pythonExecutable;

        var projectResource = new PythonProjectResource(name, projectExecutable, projectDirectory);

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
                context.Args.Add(pythonExecutable!);
            }

            context.Args.Add(scriptPath);

            foreach (var arg in scriptArgs)
            {
                context.Args.Add(arg);
            }
        });

        if(!string.IsNullOrEmpty(instrumentationExecutable))
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
