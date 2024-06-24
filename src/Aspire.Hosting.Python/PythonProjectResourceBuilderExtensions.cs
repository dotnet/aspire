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
    /// </summary>
    /// <example>
    /// Use in application host
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// builder.AddPythonProject("python-project", "PythonProject", "main.py");
    /// 
    /// builder.Build().Run(); 
    /// </code>
    /// </example>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resource to.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="projectDirectory">The path to the directory containing the python project files.</param>
    /// <param name="scriptPath">The path to the script relative to the project directory to run.</param>
    /// <param name="virtualEnvironmentPath">Path to the virtual environment.</param>
    /// <param name="scriptArgs">The arguments for the script.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// To receive traces, logs, and metrics from the python project in the dashboard, the project must be instrumented with OpenTelemetry.
    /// You can instrument your project by adding the <c>opentelemetry-distro</c>, and <c>opentelemetry-exporter-otlp</c> to
    /// your Python project.
    /// </remarks>
    public static IResourceBuilder<PythonProjectResource> AddPythonProject(
        this IDistributedApplicationBuilder builder, string name, string projectDirectory, string scriptPath, string virtualEnvironmentPath = ".venv", params string[] scriptArgs)
    {
        projectDirectory = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, projectDirectory));
        var virtualEnvironment = new VirtualEnvironment(Path.IsPathRooted(virtualEnvironmentPath)
            ? virtualEnvironmentPath
            : Path.Join(projectDirectory, virtualEnvironmentPath));

        var instrumentationExecutable = virtualEnvironment.GetExecutable("opentelemetry-instrument");
        var pythonExecutable = virtualEnvironment.GetRequiredExecutable("python");
        var projectExecutable = instrumentationExecutable ?? pythonExecutable;

        var projectResource = new PythonProjectResource(name, projectExecutable, projectDirectory);

        var resourceBuilder = builder.AddResource(projectResource).WithArgs(context =>
        {
            // If the project is to be automatically instrumented, add the instrumentation executable arguments first.
            if (!string.IsNullOrEmpty(instrumentationExecutable))
            {
                AddOpenTelemetryArguments(context);

                // Add the python executable as the next argument so we can run the project.
                context.Args.Add(pythonExecutable!);
            }

            AddProjectArguments(scriptPath, scriptArgs, context);
        });

        if(!string.IsNullOrEmpty(instrumentationExecutable))
        {
            resourceBuilder.WithOtlpExporter();

            // Make sure to attach the logging instrumentation setting, so we can capture logs.
            // Without this you'll need to configure logging yourself. Which is kind of a pain.
            resourceBuilder.WithEnvironment("OTEL_PYTHON_LOGGING_AUTO_INSTRUMENTATION_ENABLED", "true");
        }

        resourceBuilder.PublishAsDockerFile();

        return resourceBuilder;
    }

    private static void AddProjectArguments(string scriptPath, string[] scriptArgs, CommandLineArgsCallbackContext context)
    {
        context.Args.Add(scriptPath);

        foreach (var arg in scriptArgs)
        {
            context.Args.Add(arg);
        }
    }

    private static void AddOpenTelemetryArguments(CommandLineArgsCallbackContext context)
    {
        context.Args.Add("--traces_exporter");
        context.Args.Add("otlp");

        context.Args.Add("--logs_exporter");
        context.Args.Add("console,otlp");

        context.Args.Add("--metrics_exporter");
        context.Args.Add("otlp");
    }
}
