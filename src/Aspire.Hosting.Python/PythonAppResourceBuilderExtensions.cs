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
    /// builder.AddPythonApp("python-app", "../python-app", "main.py")
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
        ArgumentNullException.ThrowIfNull(appDirectory);
        ArgumentException.ThrowIfNullOrEmpty(scriptPath);
        ArgumentNullException.ThrowIfNull(virtualEnvironmentPath);

        appDirectory = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, appDirectory));
        var virtualEnvironment = new VirtualEnvironment(Path.IsPathRooted(virtualEnvironmentPath)
            ? virtualEnvironmentPath
            : Path.Join(appDirectory, virtualEnvironmentPath));

        var pythonExecutable = virtualEnvironment.GetExecutable("python");

        var resource = new PythonAppResource(name, pythonExecutable, appDirectory);

        var resourceBuilder = builder
            .AddResource(resource)
            .WithArgs(context =>
        {
            context.Args.Add(scriptPath);
        });

        resourceBuilder.WithPythonEnvironment(env =>
        {
            env.VirtualEnvironment = virtualEnvironment;
        });
        resourceBuilder.WithIconName("CodePyRectangle");

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

        // Configure required environment variables for custom certificate trust when running as an executable
        resourceBuilder.WithExecutableCertificateTrustCallback(ctx =>
        {
            if (ctx.Scope == CustomCertificateAuthoritiesScope.Override)
            {
                // See: https://docs.python-requests.org/en/latest/user/advanced/#ssl-cert-verification
                ctx.CertificateBundleEnvironment.Add("REQUESTS_CA_BUNDLE");
            }

            // Override default opentelemetry-python certificate bundle path
            // See: https://opentelemetry-python.readthedocs.io/en/latest/exporter/otlp/otlp.html#module-opentelemetry.exporter.otlp
            ctx.CertificateBundleEnvironment.Add("OTEL_EXPORTER_OTLP_TRACES_CERTIFICATE");

            return Task.CompletedTask;
        });

        resourceBuilder.WithVSCodeDebugSupport(mode => new PythonLaunchConfiguration { ProgramPath = Path.Join(appDirectory, scriptPath), Mode = mode }, "ms-python.python", ctx =>
        {
            ctx.Args.RemoveAt(0); // The first argument when running from command line is the entrypoint file.
        });

        resourceBuilder.PublishAsDockerFile(c =>
        {
            // Only generate a Dockerfile if one doesn't already exist in the app directory
            if (File.Exists(Path.Combine(appDirectory, "Dockerfile")))
            {
                return;
            }

            var entry = Path.GetFileName(scriptPath);

            c.WithDockerfileBuilder(appDirectory,
                context =>
                {
                    if (!c.Resource.TryGetLastAnnotation<PythonEnvironmentAnnotation>(out var pythonEnvironmentAnnotation) ||
                        !pythonEnvironmentAnnotation.Uv)
                    {
                        // Use the default Dockerfile if not using UV
                        return;
                    }

                    var pythonVersion = pythonEnvironmentAnnotation.Version ?? PythonVersionDetector.DetectVersion(appDirectory, pythonEnvironmentAnnotation.VirtualEnvironment!);

                    if (pythonVersion is null)
                    {
                        // Could not detect Python version, skip Dockerfile generation
                        return;
                    }

                    var builderStage = context.Builder
                        .From($"ghcr.io/astral-sh/uv:python{pythonVersion}-bookworm-slim", "builder")
                        .EmptyLine()
                        .Comment("Enable bytecode compilation and copy mode for the virtual environment")
                        .Env("UV_COMPILE_BYTECODE", "1")
                        .Env("UV_LINK_MODE", "copy")
                        .EmptyLine()
                        .WorkDir("/app")
                        .EmptyLine()
                        .Comment("Install dependencies first for better layer caching")
                        .Comment("Uses BuildKit cache mounts to speed up repeated builds")
                        .RunWithMounts(
                            "uv sync --locked --no-install-project --no-dev",
                            "type=cache,target=/root/.cache/uv",
                            "type=bind,source=uv.lock,target=uv.lock",
                            "type=bind,source=pyproject.toml,target=pyproject.toml")
                        .EmptyLine()
                        .Comment("Copy the rest of the application source and install the project")
                        .Copy(".", "/app")
                        .RunWithMounts(
                            "uv sync --locked --no-dev",
                            "type=cache,target=/root/.cache/uv");

                    context.Builder
                        .From($"python:{pythonVersion}-slim-bookworm", "app")
                        .EmptyLine()
                        .Comment("------------------------------")
                        .Comment("🚀 Runtime stage")
                        .Comment("------------------------------")
                        .Comment("Create non-root user for security")
                        .Run("groupadd --system --gid 999 appuser && useradd --system --gid 999 --uid 999 --create-home appuser")
                        .EmptyLine()
                        .Comment("Copy the application and virtual environment from builder")
                        .CopyFrom(builderStage.StageName!, "/app", "/app", "appuser:appuser")
                        .EmptyLine()
                        .Comment("Add virtual environment to PATH and set VIRTUAL_ENV")
                        .Env("PATH", "/app/.venv/bin:${PATH}")
                        .Env("VIRTUAL_ENV", "/app/.venv")
                        .Env("PYTHONDONTWRITEBYTECODE", "1")
                        .Env("PYTHONUNBUFFERED", "1")
                        .EmptyLine()
                        .Comment("Use the non-root user to run the application")
                        .User("appuser")
                        .EmptyLine()
                        .Comment("Set working directory")
                        .WorkDir("/app")
                        .EmptyLine()
                        .Comment("Run the application")
                        .Entrypoint(["python"])
                        .Cmd([entry]);
                });
        });

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
    /// <param name="virtualEnvironmentPath">
    /// The path to the virtual environment. Can be absolute or relative to the app directory.
    /// When relative, it is resolved from the working directory of the Python application.
    /// Common values include ".venv", "venv", or "myenv".
    /// </param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method updates the Python executable path to use the specified virtual environment.
    /// The virtual environment must already exist and be properly initialized before the application runs.
    /// </para>
    /// <para>
    /// Virtual environments allow Python applications to have isolated dependencies separate from
    /// the system Python installation. This is the recommended approach for Python applications.
    /// </para>
    /// </remarks>
    /// <example>
    /// Configure a Python app to use a custom virtual environment:
    /// <code lang="csharp">
    /// var python = builder.AddPythonApp("api", "../python-api", "main.py")
    ///     .WithVirtualEnvironment("myenv");
    /// </code>
    /// </example>
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
        builder.WithPythonEnvironment(env =>
        {
            env.VirtualEnvironment = virtualEnvironment;
        });

        return builder;
    }

    /// <summary>
    /// Adds a UV environment setup task to ensure the virtual environment exists before running the Python application.
    /// </summary>
    /// <typeparam name="T">The type of the Python application resource, must derive from <see cref="PythonAppResource"/>.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method creates a child resource that runs <c>uv sync</c> in the working directory of the Python application.
    /// The Python application will wait for this resource to complete successfully before starting.
    /// </para>
    /// <para>
    /// UV (https://github.com/astral-sh/uv) is a modern Python package manager written in Rust that can manage virtual environments
    /// and dependencies with significantly faster performance than traditional tools. The <c>uv sync</c> command ensures that the virtual
    /// environment exists and all dependencies specified in pyproject.toml are installed and synchronized.
    /// </para>
    /// <para>
    /// This method is idempotent - calling it multiple times on the same resource will not create duplicate UV environment resources.
    /// If a UV environment resource already exists for the Python application, it will be reused.
    /// </para>
    /// </remarks>
    /// <example>
    /// Add a Python app with automatic UV environment setup:
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var python = builder.AddPythonApp("api", "../python-api", "main.py")
    ///     .WithUvEnvironment()  // Automatically runs 'uv sync' before starting the app
    ///     .WithHttpEndpoint(port: 5000);
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="DistributedApplicationException">
    /// Thrown when a resource with the UV environment name already exists but is not a <see cref="PythonUvEnvironmentResource"/>.
    /// </exception>
    public static IResourceBuilder<T> WithUvEnvironment<T>(this IResourceBuilder<T> builder)
        where T : PythonAppResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        var uvEnvironmentName = $"{builder.Resource.Name}-uv-environment";

        // Check if the UV environment resource already exists
        var existingResource = builder.ApplicationBuilder.Resources
            .FirstOrDefault(r => string.Equals(r.Name, uvEnvironmentName, StringComparison.OrdinalIgnoreCase));

        IResourceBuilder<PythonUvEnvironmentResource> uvBuilder;

        if (existingResource is not null)
        {
            // Resource already exists, return a builder for it
            if (existingResource is not PythonUvEnvironmentResource uvEnvironmentResource)
            {
                throw new DistributedApplicationException($"Cannot add UV environment resource with name '{uvEnvironmentName}' because a resource of type '{existingResource.GetType()}' with that name already exists.");
            }

            uvBuilder = builder.ApplicationBuilder.CreateResourceBuilder(uvEnvironmentResource);
        }
        else
        {
            // Resource doesn't exist, create it
            var uvEnvironmentResource = new PythonUvEnvironmentResource(uvEnvironmentName, builder.Resource);

            uvBuilder = builder.ApplicationBuilder.AddResource(uvEnvironmentResource)
                .WithArgs("sync")
                .WithParentRelationship(builder)
                .ExcludeFromManifest();

            builder.WaitForCompletion(uvBuilder)
                   .WithPythonEnvironment(env => env.Uv = true);
        }

        return builder;
    }

    internal static IResourceBuilder<PythonAppResource> WithPythonEnvironment(this IResourceBuilder<PythonAppResource> builder, Action<PythonEnvironmentAnnotation> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        if (!builder.Resource.TryGetLastAnnotation<PythonEnvironmentAnnotation>(out var existing))
        {
            existing = new PythonEnvironmentAnnotation();
            builder.WithAnnotation(existing);
        }

        configure(existing);

        return builder;
    }
}
