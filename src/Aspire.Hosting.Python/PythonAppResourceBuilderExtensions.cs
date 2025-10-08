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
    /// Use <see cref="WithScriptArgs(IResourceBuilder{PythonAppResource}, string[])"/> to pass arguments to the script.
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
    ///        .WithScriptArgs("arg1", "arg2");
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    /// </remarks>
    [OverloadResolutionPriority(1)]
    public static IResourceBuilder<PythonAppResource> AddPythonApp(
        this IDistributedApplicationBuilder builder, [ResourceName] string name, string appDirectory, string scriptPath)
        => AddPythonAppCore(builder, name, appDirectory, scriptPath, ".venv", []);

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
    /// This overload is obsolete. Use the overload without parameters and chain with <c>WithScriptArgs</c>:
    /// <code lang="csharp">
    /// builder.AddPythonApp("name", "dir", "script.py")
    ///        .WithScriptArgs("arg1", "arg2");
    /// </code>
    /// </para>
    /// </remarks>
    [Obsolete("Use AddPythonApp(builder, name, appDirectory, scriptPath) and chain with .WithScriptArgs(...) instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IResourceBuilder<PythonAppResource> AddPythonApp(
        this IDistributedApplicationBuilder builder, string name, string appDirectory, string scriptPath, params string[] scriptArgs)
        => AddPythonAppCore(builder, name, appDirectory, scriptPath, ".venv", scriptArgs);

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
    /// This overload is obsolete. Use the overload without parameters and chain with <c>WithVirtualEnvironment</c> and <c>WithScriptArgs</c>:
    /// <code lang="csharp">
    /// builder.AddPythonApp("name", "dir", "script.py")
    ///        .WithVirtualEnvironment("myenv")
    ///        .WithScriptArgs("arg1", "arg2");
    /// </code>
    /// </para>
    /// </remarks>
    [Obsolete("Use AddPythonApp(builder, name, appDirectory, scriptPath) and chain with .WithVirtualEnvironment(...).WithScriptArgs(...) instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IResourceBuilder<PythonAppResource> AddPythonApp(
        this IDistributedApplicationBuilder builder, string name, string appDirectory, string scriptPath,
        string virtualEnvironmentPath, params string[] scriptArgs)
        => AddPythonAppCore(builder, name, appDirectory, scriptPath, virtualEnvironmentPath, scriptArgs);

    private static IResourceBuilder<PythonAppResource> AddPythonAppCore(
        IDistributedApplicationBuilder builder, string name, string appDirectory, string scriptPath,
        string virtualEnvironmentPath, string[] scriptArgs)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(appDirectory);
        ArgumentException.ThrowIfNullOrEmpty(scriptPath);
        ArgumentException.ThrowIfNullOrEmpty(virtualEnvironmentPath);
        ThrowIfNullOrContainsIsNullOrEmpty(scriptArgs);

        appDirectory = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, appDirectory));
        var virtualEnvironment = new VirtualEnvironment(Path.IsPathRooted(virtualEnvironmentPath)
            ? virtualEnvironmentPath
            : Path.Join(appDirectory, virtualEnvironmentPath));

        var instrumentationExecutable = virtualEnvironment.GetExecutable("opentelemetry-instrument");
        var pythonExecutable = virtualEnvironment.GetRequiredExecutable("python");
        var appExecutable = instrumentationExecutable ?? pythonExecutable;

        var resource = new PythonAppResource(name, appExecutable, appDirectory);

        var resourceBuilder = builder.AddResource(resource).WithArgs(context =>
        {
            // If the app is to be automatically instrumented, add the instrumentation executable arguments first.
            if (!string.IsNullOrEmpty(instrumentationExecutable))
            {
                AddOpenTelemetryArguments(context);

                // Add the python executable as the next argument so we can run the app.
                context.Args.Add(pythonExecutable!);
            }

            AddArguments(scriptPath, scriptArgs, context);
        });

        if (!string.IsNullOrEmpty(instrumentationExecutable))
        {
            resourceBuilder.WithOtlpExporter();

            // Make sure to attach the logging instrumentation setting, so we can capture logs.
            // Without this you'll need to configure logging yourself. Which is kind of a pain.
            resourceBuilder.WithEnvironment("OTEL_PYTHON_LOGGING_AUTO_INSTRUMENTATION_ENABLED", "true");
        }

        resourceBuilder.WithVSCodeDebugSupport(mode => new PythonLaunchConfiguration { ProgramPath = Path.Join(appDirectory, scriptPath), Mode = mode }, "ms-python.python", ctx =>
        {
            ctx.Args.RemoveAt(0); // The first argument when running from command line is the entrypoint file.
        });

        resourceBuilder.PublishAsDockerFile();

        return resourceBuilder;
    }

    private static void AddArguments(string scriptPath, string[] scriptArgs, CommandLineArgsCallbackContext context)
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
    /// <remarks>
    /// <para>
    /// This method can only be used with Python app resources that haven't been fully configured yet.
    /// Due to architectural limitations, changing the virtual environment after resource creation requires using
    /// the deprecated <see cref="AddPythonApp(IDistributedApplicationBuilder, string, string, string, string, string[])"/> overload directly.
    /// </para>
    /// </remarks>
    [Obsolete("WithVirtualEnvironment cannot be used after resource creation. Use the AddPythonApp overload with virtualEnvironmentPath parameter instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IResourceBuilder<PythonAppResource> WithVirtualEnvironment(
        this IResourceBuilder<PythonAppResource> builder, string virtualEnvironmentPath)
    {
        throw new NotSupportedException(
            "WithVirtualEnvironment cannot modify the virtual environment after the Python resource has been created. " +
            "The virtual environment path must be specified when calling AddPythonApp.");
    }

    /// <summary>
    /// Adds arguments to pass to the Python script.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="args">The arguments to pass to the script.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<PythonAppResource> WithScriptArgs(
        this IResourceBuilder<PythonAppResource> builder, params string[] args)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ThrowIfNullOrContainsIsNullOrEmpty(args);

        return builder.WithArgs(args);
    }
}
