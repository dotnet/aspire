// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
#pragma warning disable ASPIREEXTENSION001
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Python;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Python applications to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class PythonAppResourceBuilderExtensions
{
    /// <summary>
    /// Adds a python application to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resource to.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="appDirectory">The path to the directory containing the python app files.</param>
    /// <param name="scriptPath">The path to the script relative to the app directory to run.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <para>
    /// By default, the virtual environment folder is expected to be named <c>.venv</c> and located in the app directory.
    /// Use <see cref="WithVirtualEnvironment(IResourceBuilder{PythonAppResource}, string)"/> to specify a different virtual environment path.
    /// Use <c>WithArgs</c> to pass arguments to the script.
    /// </para>
    /// <para>
    /// The virtual environment must be initialized before running the app. To setup a virtual environment use the 
    /// <c>python -m venv .venv</c> command in the app directory.
    /// </para>
    /// <para>
    /// To restore dependencies in the virtual environment first activate the environment by executing the activation
    /// script and then use the <c>pip install -r requirements.txt</c> command to restore dependencies.
    /// </para>
    /// <para>
    /// To receive traces, logs, and metrics from the python app in the dashboard, the app must be instrumented with OpenTelemetry.
    /// You can instrument your app by adding the <c>opentelemetry-distro</c>, and <c>opentelemetry-exporter-otlp</c> to
    /// your Python app.
    /// </para>
    /// <example>
    /// Add a python app to the application model:
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// builder.AddPythonApp("python-app", "PythonApp", "main.py")
    ///        .WithArgs("arg1", "arg2");
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    /// </remarks>
    [OverloadResolutionPriority(1)]
    public static IResourceBuilder<PythonAppResource> AddPythonApp(
        this IDistributedApplicationBuilder builder, [ResourceName] string name, string appDirectory, string scriptPath)
        => AddPythonAppCore(builder, name, appDirectory, scriptPath, ".venv");

    /// <summary>
    /// Adds a python application with a virtual environment to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resource to.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="appDirectory">The path to the directory containing the python app files.</param>
    /// <param name="scriptPath">The path to the script relative to the app directory to run.</param>
    /// <param name="scriptArgs">The arguments for the script.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This overload is obsolete. Use the overload without parameters and chain with <c>WithArgs</c>:
    /// <code lang="csharp">
    /// builder.AddPythonApp("name", "dir", "script.py")
    ///        .WithArgs("arg1", "arg2");
    /// </code>
    /// </para>
    /// </remarks>
    [Obsolete("Use AddPythonApp(builder, name, appDirectory, scriptPath) and chain with .WithArgs(...) instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IResourceBuilder<PythonAppResource> AddPythonApp(
        this IDistributedApplicationBuilder builder, string name, string appDirectory, string scriptPath, params string[] scriptArgs)
    {
        ThrowIfNullOrContainsIsNullOrEmpty(scriptArgs);
        return AddPythonAppCore(builder, name, appDirectory, scriptPath, ".venv")
            .WithArgs(scriptArgs);
    }

    /// <summary>
    /// Adds a python application with a virtual environment to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resource to.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="appDirectory">The path to the directory containing the python app files.</param>
    /// <param name="scriptPath">The path to the script to run, relative to the app directory.</param>
    /// <param name="virtualEnvironmentPath">Path to the virtual environment.</param>
    /// <param name="scriptArgs">The arguments for the script.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This overload is obsolete. Use the overload without parameters and chain with <c>WithVirtualEnvironment</c> and <c>WithArgs</c>:
    /// <code lang="csharp">
    /// builder.AddPythonApp("name", "dir", "script.py")
    ///        .WithVirtualEnvironment("myenv")
    ///        .WithArgs("arg1", "arg2");
    /// </code>
    /// </para>
    /// </remarks>
    [Obsolete("Use AddPythonApp(builder, name, appDirectory, scriptPath) and chain with .WithVirtualEnvironment(...).WithArgs(...) instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IResourceBuilder<PythonAppResource> AddPythonApp(
        this IDistributedApplicationBuilder builder, string name, string appDirectory, string scriptPath,
        string virtualEnvironmentPath, params string[] scriptArgs)
    {
        ThrowIfNullOrContainsIsNullOrEmpty(scriptArgs);
        return AddPythonAppCore(builder, name, appDirectory, scriptPath, virtualEnvironmentPath)
            .WithArgs(scriptArgs);
    }

    private static IResourceBuilder<PythonAppResource> AddPythonAppCore(
        IDistributedApplicationBuilder builder, string name, string appDirectory, string scriptPath,
        string virtualEnvironmentPath)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(appDirectory);
        ArgumentException.ThrowIfNullOrEmpty(scriptPath);
        ArgumentException.ThrowIfNullOrEmpty(virtualEnvironmentPath);

        appDirectory = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, appDirectory));
        var virtualEnvironment = new VirtualEnvironment(Path.IsPathRooted(virtualEnvironmentPath)
            ? virtualEnvironmentPath
            : Path.Join(appDirectory, virtualEnvironmentPath));

        var pythonExecutable = virtualEnvironment.GetExecutable("python");

        var resource = new PythonAppResource(name, pythonExecutable, appDirectory);

        var resourceBuilder = builder.AddResource(resource).WithArgs(context =>
        {
            context.Args.Add(scriptPath);
        });

        resourceBuilder.WithOtlpExporter();

        // Configure OpenTelemetry exporters using environment variables
        // https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/#exporter-selection
        resourceBuilder.WithEnvironment(context =>
        {
            context.EnvironmentVariables["OTEL_TRACES_EXPORTER"] = "otlp";
            context.EnvironmentVariables["OTEL_LOGS_EXPORTER"] = "otlp";
            context.EnvironmentVariables["OTEL_METRICS_EXPORTER"] = "otlp";

            // Make sure to attach the logging instrumentation setting, so we can capture logs.
            // Without this you'll need to configure logging yourself. Which is kind of a pain.
            context.EnvironmentVariables["OTEL_PYTHON_LOGGING_AUTO_INSTRUMENTATION_ENABLED"] = "true";
        });

        resourceBuilder.WithVSCodeDebugSupport(mode => new PythonLaunchConfiguration { ProgramPath = Path.Join(appDirectory, scriptPath), Mode = mode }, "ms-python.python", ctx =>
        {
            ctx.Args.RemoveAt(0); // The first argument when running from command line is the entrypoint file.
        });

        resourceBuilder.PublishAsDockerFile();

        return resourceBuilder;
    }

    private static void ThrowIfNullOrContainsIsNullOrEmpty(string[] scriptArgs)
    {
        ArgumentNullException.ThrowIfNull(scriptArgs);
        foreach (var scriptArg in scriptArgs)
        {
            if (string.IsNullOrEmpty(scriptArg))
            {
                var values = string.Join(", ", scriptArgs);
                if (scriptArg is null)
                {
                    throw new ArgumentNullException(nameof(scriptArgs), $"Array params contains null item: [{values}]");
                }
                throw new ArgumentException($"Array params contains empty item: [{values}]", nameof(scriptArgs));
            }
        }
    }

    /// <summary>
    /// Configures a custom virtual environment path for the Python application.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="virtualEnvironmentPath">The path to the virtual environment. Can be absolute or relative to the app directory.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<PythonAppResource> WithVirtualEnvironment(
        this IResourceBuilder<PythonAppResource> builder, string virtualEnvironmentPath)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(virtualEnvironmentPath);

        var virtualEnvironment = new VirtualEnvironment(Path.IsPathRooted(virtualEnvironmentPath)
            ? virtualEnvironmentPath
            : Path.Join(builder.Resource.WorkingDirectory, virtualEnvironmentPath));
        // Update the command to use the new virtual environment
        builder.WithCommand(virtualEnvironment.GetExecutable("python"));

        return builder;
    }
}
