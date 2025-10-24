// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.ApplicationModel.Docker;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Python;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#pragma warning disable ASPIREEXTENSION001

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Python applications to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class PythonAppResourceBuilderExtensions
{
    private const string DefaultVirtualEnvFolder = ".venv";

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
    /// This method is obsolete. Use one of the more specific methods instead:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="AddPythonScript"/> - To run a Python script file</description></item>
    /// <item><description><see cref="AddPythonModule"/> - To run a Python module via <c>python -m</c></description></item>
    /// <item><description><see cref="AddPythonExecutable"/> - To run an executable from the virtual environment</description></item>
    /// </list>
    /// <para>
    /// These new methods provide better clarity about how the Python application will be executed.
    /// You can also use <see cref="WithEntrypoint"/> to change the entrypoint type after creation.
    /// </para>
    /// </remarks>
    /// <example>
    /// Replace with <see cref="AddPythonScript"/>:
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// builder.AddPythonScript("python-app", "../python-app", "main.py")
    ///        .WithArgs("arg1", "arg2");
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    [Obsolete("Use AddPythonScript, AddPythonModule, or AddPythonExecutable instead for more explicit control over how the Python application is executed.")]
    [OverloadResolutionPriority(1)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IResourceBuilder<PythonAppResource> AddPythonApp(
        this IDistributedApplicationBuilder builder, [ResourceName] string name, string appDirectory, string scriptPath)
        => AddPythonAppCore(builder, name, appDirectory, EntrypointType.Script, scriptPath, DefaultVirtualEnvFolder);

    /// <summary>
    /// Adds a Python script to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resource to.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="appDirectory">The path to the directory containing the python script.</param>
    /// <param name="scriptPath">The path to the script relative to the app directory to run.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method executes a Python script directly using <c>python script.py</c>.
    /// By default, the virtual environment folder is expected to be named <c>.venv</c> and located in the app directory.
    /// Use <see cref="WithVirtualEnvironment(IResourceBuilder{PythonAppResource}, string)"/> to specify a different virtual environment path.
    /// Use <c>WithArgs</c> to pass arguments to the script.
    /// </para>
    /// </remarks>
    /// <example>
    /// Add a FastAPI Python script to the application model:
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// builder.AddPythonScript("fastapi-app", "../api", "main.py")
    ///        .WithArgs("arg1", "arg2");
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<PythonAppResource> AddPythonScript(
        this IDistributedApplicationBuilder builder, [ResourceName] string name, string appDirectory, string scriptPath)
        => AddPythonAppCore(builder, name, appDirectory, EntrypointType.Script, scriptPath, DefaultVirtualEnvFolder);

    /// <summary>
    /// Adds a Python module to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resource to.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="appDirectory">The path to the directory containing the python application.</param>
    /// <param name="moduleName">The name of the Python module to run (e.g., "flask", "uvicorn").</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method runs a Python module using <c>python -m &lt;module&gt;</c>.
    /// By default, the virtual environment folder is expected to be named <c>.venv</c> and located in the app directory.
    /// Use <see cref="WithVirtualEnvironment(IResourceBuilder{PythonAppResource}, string)"/> to specify a different virtual environment path.
    /// Use <c>WithArgs</c> to pass arguments to the module.
    /// </para>
    /// </remarks>
    /// <example>
    /// Add a Flask module to the application model:
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// builder.AddPythonModule("flask-dev", "../flaskapp", "flask")
    ///        .WithArgs("run", "--debug", "--host=0.0.0.0");
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<PythonAppResource> AddPythonModule(
        this IDistributedApplicationBuilder builder, [ResourceName] string name, string appDirectory, string moduleName)
        => AddPythonAppCore(builder, name, appDirectory, EntrypointType.Module, moduleName, DefaultVirtualEnvFolder);

    /// <summary>
    /// Adds a Python executable to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resource to.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="appDirectory">The path to the directory containing the python application.</param>
    /// <param name="executableName">The name of the executable in the virtual environment (e.g., "pytest", "uvicorn", "flask").</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method runs an executable from the virtual environment's bin directory.
    /// By default, the virtual environment folder is expected to be named <c>.venv</c> and located in the app directory.
    /// Use <see cref="WithVirtualEnvironment(IResourceBuilder{PythonAppResource}, string)"/> to specify a different virtual environment path.
    /// Use <c>WithArgs</c> to pass arguments to the executable.
    /// </para>
    /// </remarks>
    /// <example>
    /// Add a pytest executable to the application model:
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// builder.AddPythonExecutable("pytest", "../api", "pytest")
    ///        .WithArgs("-q");
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<PythonAppResource> AddPythonExecutable(
        this IDistributedApplicationBuilder builder, [ResourceName] string name, string appDirectory, string executableName)
        => AddPythonAppCore(builder, name, appDirectory, EntrypointType.Executable, executableName, DefaultVirtualEnvFolder);

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
    /// This overload is obsolete. Use one of the more specific methods instead:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="AddPythonScript"/> - To run a Python script file</description></item>
    /// <item><description><see cref="AddPythonModule"/> - To run a Python module via <c>python -m</c></description></item>
    /// <item><description><see cref="AddPythonExecutable"/> - To run an executable from the virtual environment</description></item>
    /// </list>
    /// <para>
    /// Chain with <c>WithArgs</c> to pass arguments:
    /// </para>
    /// <example>
    /// <code lang="csharp">
    /// builder.AddPythonScript("name", "dir", "script.py")
    ///        .WithArgs("arg1", "arg2");
    /// </code>
    /// </example>
    /// </remarks>
    [Obsolete("Use AddPythonScript, AddPythonModule, or AddPythonExecutable and chain with .WithArgs(...) instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IResourceBuilder<PythonAppResource> AddPythonApp(
        this IDistributedApplicationBuilder builder, string name, string appDirectory, string scriptPath, params string[] scriptArgs)
    {
        ArgumentException.ThrowIfNullOrEmpty(scriptPath);
        ThrowIfNullOrContainsIsNullOrEmpty(scriptArgs);
        return AddPythonAppCore(builder, name, appDirectory, EntrypointType.Script, scriptPath, DefaultVirtualEnvFolder)
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
    /// This overload is obsolete. Use one of the more specific methods instead:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="AddPythonScript"/> - To run a Python script file</description></item>
    /// <item><description><see cref="AddPythonModule"/> - To run a Python module via <c>python -m</c></description></item>
    /// <item><description><see cref="AddPythonExecutable"/> - To run an executable from the virtual environment</description></item>
    /// </list>
    /// <para>
    /// Chain with <see cref="WithVirtualEnvironment"/> and <c>WithArgs</c>:
    /// </para>
    /// <example>
    /// <code lang="csharp">
    /// builder.AddPythonScript("name", "dir", "script.py")
    ///        .WithVirtualEnvironment("myenv")
    ///        .WithArgs("arg1", "arg2");
    /// </code>
    /// </example>
    /// </remarks>
    [Obsolete("Use AddPythonScript, AddPythonModule, or AddPythonExecutable and chain with .WithVirtualEnvironment(...).WithArgs(...) instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IResourceBuilder<PythonAppResource> AddPythonApp(
        this IDistributedApplicationBuilder builder, string name, string appDirectory, string scriptPath,
        string virtualEnvironmentPath, params string[] scriptArgs)
    {
        ThrowIfNullOrContainsIsNullOrEmpty(scriptArgs);
        ArgumentException.ThrowIfNullOrEmpty(scriptPath);
        return AddPythonAppCore(builder, name, appDirectory, EntrypointType.Script, scriptPath, virtualEnvironmentPath)
            .WithArgs(scriptArgs);
    }

    /// <summary>
    /// Adds a Uvicorn-based Python application to the distributed application builder with HTTP endpoint configuration.
    /// </summary>
    /// <remarks>This method configures the application to use Uvicorn as the server and exposes an HTTP
    /// endpoint. When publishing, it sets the entry point to use the Uvicorn executable with appropriate arguments for
    /// host and port.</remarks>
    /// <param name="builder">The distributed application builder to which the Uvicorn application resource will be added.</param>
    /// <param name="name">The unique name of the Uvicorn application resource.</param>
    /// <param name="appDirectory">The directory containing the Python application files.</param>
    /// <param name="app">The ASGI app import path which informs Uvicorn which module and variable to load as your web application.
    /// For example, "main:app" means "main.py" file and variable named "app".</param>
    /// <returns>A resource builder for further configuration of the Uvicorn Python application resource.</returns>
    public static IResourceBuilder<PythonAppResource> AddUvicornApp(
        this IDistributedApplicationBuilder builder, [ResourceName] string name, string appDirectory, string app)
    {
        var resourceBuilder = builder.AddPythonExecutable(name, appDirectory, "uvicorn")
            .WithHttpEndpoint(env: "PORT")
            .WithArgs(c =>
            {
                c.Args.Add(app);

                c.Args.Add("--host");
                var endpoint = ((IResourceWithEndpoints)c.Resource).GetEndpoint("http");
                if (builder.ExecutionContext.IsPublishMode)
                {
                    c.Args.Add("0.0.0.0");
                }
                else
                {
                    c.Args.Add(endpoint.EndpointAnnotation.TargetHost);
                }

                c.Args.Add("--port");
                c.Args.Add(endpoint.Property(EndpointProperty.TargetPort));
            });

        return resourceBuilder;
    }

    private static IResourceBuilder<PythonAppResource> AddPythonAppCore(
        IDistributedApplicationBuilder builder, string name, string appDirectory, EntrypointType entrypointType,
        string entrypoint, string virtualEnvironmentPath)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(appDirectory);
        ArgumentException.ThrowIfNullOrEmpty(entrypoint);
        ArgumentNullException.ThrowIfNull(virtualEnvironmentPath);

        // python will be replaced with the resolved entrypoint based on the virtualEnvironmentPath
        var resource = new PythonAppResource(name, "python", Path.GetFullPath(appDirectory, builder.AppHostDirectory));

        var resourceBuilder = builder
            .AddResource(resource)
            // Order matters, we need to bootstrap the entrypoint before setting the entrypoint
            .WithAnnotation(new PythonEntrypointAnnotation
            {
                Type = entrypointType,
                Entrypoint = entrypoint
            })
            // This will resolve the correct python executable based on the virtual environment
            .WithVirtualEnvironment(virtualEnvironmentPath)
            // This will set up the the entrypoint based on the PythonEntrypointAnnotation
            .WithEntrypoint(entrypointType, entrypoint);

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

            // Set PYTHONUTF8=1 on Windows in run mode to enable UTF-8 mode
            // See: https://docs.python.org/3/using/cmdline.html#envvar-PYTHONUTF8
            if (OperatingSystem.IsWindows() && context.ExecutionContext.IsRunMode)
            {
                context.EnvironmentVariables["PYTHONUTF8"] = "1";
            }
        });

        // Configure required environment variables for custom certificate trust when running as an executable
        // Python defaults to using System scope to allow combining custom CAs with system CAs as there's no clean
        // way to simply append additional certificates to default Python trust stores such as certifi.
        resourceBuilder
            .WithCertificateTrustScope(CertificateTrustScope.System)
            .WithExecutableCertificateTrustCallback(ctx =>
            {
                if (ctx.Scope == CertificateTrustScope.Append)
                {
                    var resourceLogger = ctx.ExecutionContext.ServiceProvider.GetRequiredService<ResourceLoggerService>();
                    var logger = resourceLogger.GetLogger(ctx.Resource);
                    logger.LogWarning("Certificate trust scope is set to 'Append', but Python resources do not support appending to the default certificate authorities; only OTLP certificate trust will be applied. Consider using 'System' or 'Override' certificate trust scopes instead.");
                }
                else
                {
                    // Override default certificates path for the requests module.
                    // See: https://docs.python-requests.org/en/latest/user/advanced/#ssl-cert-verification
                    ctx.CertificateBundleEnvironment.Add("REQUESTS_CA_BUNDLE");

                    // Requests also supports CURL_CA_BUNDLE as an alternative config (lower priority than REQUESTS_CA_BUNDLE).
                    // Setting it to be as complete as possible and avoid potential issues with conflicting configurations.
                    ctx.CertificateBundleEnvironment.Add("CURL_CA_BUNDLE");

                    // Override default certificates path for Python modules that honor OpenSSL style paths.
                    // This has been tested with urllib, urllib3, httpx, and aiohttp.
                    // See: https://docs.openssl.org/3.0/man3/SSL_CTX_load_verify_locations/#description
                    ctx.CertificateBundleEnvironment.Add("SSL_CERT_FILE");
                }

                // Override default opentelemetry-python certificate bundle path
                // See: https://opentelemetry-python.readthedocs.io/en/latest/exporter/otlp/otlp.html#module-opentelemetry.exporter.otlp
                ctx.CertificateBundleEnvironment.Add("OTEL_EXPORTER_OTLP_CERTIFICATE");

                return Task.CompletedTask;
            });

        // VS Code debug support - only applicable for Script and Module types
        if (entrypointType is EntrypointType.Script or EntrypointType.Module)
        {
            var programPath = entrypointType == EntrypointType.Script
                ? Path.GetFullPath(entrypoint, resource.WorkingDirectory)
                : null; // For modules, we'll use the module name

            resourceBuilder.WithVSCodeDebugSupport(
                mode => new PythonLaunchConfiguration
                {
                    ProgramPath = programPath,
                    Module = entrypointType == EntrypointType.Module ? entrypoint : null,
                    Mode = mode
                },
                "ms-python.python",
                static ctx =>
                {
                    // Remove entrypoint-specific arguments that VS Code will handle.
                    // We need to verify the annotation to ensure we remove the correct args.
                    if (!ctx.Resource.TryGetLastAnnotation<PythonEntrypointAnnotation>(out var annotation))
                    {
                        return;
                    }

                    // For Module type: remove "-m" and module name (2 args)
                    if (annotation.Type == EntrypointType.Module)
                    {
                        if (ctx.Args is [string arg0, string arg1, ..] &&
                            arg0 == "-m" &&
                            arg1 == annotation.Entrypoint)
                        {
                            ctx.Args.RemoveAt(0); // Remove "-m"
                            ctx.Args.RemoveAt(0); // Remove module name
                        }
                    }
                    // For Script type: remove script path (1 arg)
                    else if (annotation.Type == EntrypointType.Script)
                    {
                        if (ctx.Args is [string arg0, ..] &&
                            arg0 == annotation.Entrypoint)
                        {
                            ctx.Args.RemoveAt(0); // Remove script path
                        }
                    }
                });
        }

        resourceBuilder.PublishAsDockerFile(c =>
        {
            // Only generate a Dockerfile if one doesn't already exist in the app directory
            if (File.Exists(Path.Combine(resource.WorkingDirectory, "Dockerfile")))
            {
                return;
            }

            c.WithDockerfileBuilder(resource.WorkingDirectory,
                context =>
                {
                    if (!c.Resource.TryGetLastAnnotation<PythonEnvironmentAnnotation>(out var pythonEnvironmentAnnotation) ||
                        !pythonEnvironmentAnnotation.Uv)
                    {
                        // Use the default Dockerfile if not using UV
                        return;
                    }

                    if (!context.Resource.TryGetLastAnnotation<PythonEntrypointAnnotation>(out var entrypointAnnotation))
                    {
                        // No entrypoint annotation found, cannot generate Dockerfile
                        return;
                    }

                    var pythonVersion = pythonEnvironmentAnnotation.Version ?? PythonVersionDetector.DetectVersion(appDirectory, pythonEnvironmentAnnotation.VirtualEnvironment!);

                    if (pythonVersion is null)
                    {
                        // Could not detect Python version, skip Dockerfile generation
                        return;
                    }

                    var entrypointType = entrypointAnnotation.Type;
                    var entrypoint = entrypointAnnotation.Entrypoint;

                    // Determine entry command for Dockerfile
                    string[] entryCommand = entrypointType switch
                    {
                        EntrypointType.Script => ["python", entrypoint],
                        EntrypointType.Module => ["python", "-m", entrypoint],
                        EntrypointType.Executable => [entrypoint],
                        _ => throw new InvalidOperationException($"Unsupported entrypoint type: {entrypointType}")
                    };

                    // Check if uv.lock exists in the working directory
                    var uvLockPath = Path.Combine(resource.WorkingDirectory, "uv.lock");
                    var hasUvLock = File.Exists(uvLockPath);

                    var builderStage = context.Builder
                        .From($"ghcr.io/astral-sh/uv:python{pythonVersion}-bookworm-slim", "builder")
                        .EmptyLine()
                        .Comment("Enable bytecode compilation and copy mode for the virtual environment")
                        .Env("UV_COMPILE_BYTECODE", "1")
                        .Env("UV_LINK_MODE", "copy")
                        .EmptyLine()
                        .WorkDir("/app")
                        .EmptyLine();

                    if (hasUvLock)
                    {
                        // If uv.lock exists, use locked mode for reproducible builds
                        builderStage
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
                    }
                    else
                    {
                        // If uv.lock doesn't exist, copy pyproject.toml and generate lock file
                        builderStage
                            .Comment("Copy pyproject.toml to install dependencies")
                            .Copy("pyproject.toml", "/app/")
                            .EmptyLine()
                            .Comment("Install dependencies and generate lock file")
                            .Comment("Uses BuildKit cache mount to speed up repeated builds")
                            .RunWithMounts(
                                "uv sync --no-install-project --no-dev",
                                "type=cache,target=/root/.cache/uv")
                            .EmptyLine()
                            .Comment("Copy the rest of the application source and install the project")
                            .Copy(".", "/app")
                            .RunWithMounts(
                                "uv sync --no-dev",
                                "type=cache,target=/root/.cache/uv");
                    }

                    var runtimeBuilder = context.Builder
                        .From($"python:{pythonVersion}-slim-bookworm", "app")
                        .EmptyLine()
                        .AddStaticFiles(context.Resource, "/app")
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
                        .Comment("Run the application");

                    // Set the appropriate entrypoint and command based on entrypoint type
                    switch (entrypointType)
                    {
                        case EntrypointType.Script:
                            runtimeBuilder.Entrypoint(["python", entrypoint]);
                            break;
                        case EntrypointType.Module:
                            runtimeBuilder.Entrypoint(["python", "-m", entrypoint]);
                            break;
                        case EntrypointType.Executable:
                            runtimeBuilder.Entrypoint([entrypoint]);
                            break;
                    }
                });
        });

        resourceBuilder.WithPipelineStepFactory(factoryContext =>
        {
            List<PipelineStep> steps = [];
            var buildStep = CreateBuildImageBuildStep($"{factoryContext.Resource.Name}-build-compute", factoryContext.Resource);
            steps.Add(buildStep);

            // ensure any static file references' images are built first
            if (factoryContext.Resource.TryGetAnnotationsOfType<ContainerFilesDestinationAnnotation>(out var containerFilesAnnotations))
            {
                foreach (var containerFile in containerFilesAnnotations)
                {
                    var source = containerFile.Source;
                    var staticFileBuildStep = CreateBuildImageBuildStep($"{factoryContext.Resource.Name}-{source.Name}-build-compute", source);
                    buildStep.DependsOn(staticFileBuildStep);
                    steps.Add(staticFileBuildStep);
                }
            }

            return steps;
        });

        return resourceBuilder;
    }

    private static PipelineStep CreateBuildImageBuildStep(string stepName, IResource resource) =>
        new()
        {
            Name = stepName,
            Action = async ctx =>
            {
                var containerImageBuilder = ctx.Services.GetRequiredService<IResourceContainerImageBuilder>();
                await containerImageBuilder.BuildImageAsync(
                    resource,
                    new ContainerBuildOptions
                    {
                        TargetPlatform = ContainerTargetPlatform.LinuxAmd64
                    },
                    ctx.CancellationToken).ConfigureAwait(false);
            },
            Tags = [WellKnownPipelineTags.BuildCompute]
        };

    private static DockerfileStage AddStaticFiles(this DockerfileStage stage, IResource resource, string rootDestinationPath)
    {
        if (resource.TryGetAnnotationsOfType<ContainerFilesDestinationAnnotation>(out var containerFilesDestinationAnnotations))
        {
            foreach (var containerFileDestination in containerFilesDestinationAnnotations)
            {
                // get image name
                if (!containerFileDestination.Source.TryGetContainerImageName(out var imageName))
                {
                    throw new InvalidOperationException("Cannot add container files: Source resource does not have a container image name.");
                }

                // get the source path
                if (!containerFileDestination.Source.TryGetLastAnnotation<ContainerFilesSourceAnnotation>(out var containerFilesSource))
                {
                    throw new InvalidOperationException("Cannot add container files: Source resource does not have a ContainerFilesSourceAnnotation.");
                }

                var destinationPath = containerFileDestination.DestinationPath;
                if (!destinationPath.StartsWith('/'))
                {
                    destinationPath = $"{rootDestinationPath}/{destinationPath}";
                }
                stage.CopyFrom(imageName, containerFilesSource.SourcePath, destinationPath);
            }

            stage.EmptyLine();
        }
        return stage;
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
            : Path.GetFullPath(virtualEnvironmentPath, builder.Resource.WorkingDirectory));

        // Get the entrypoint annotation to determine how to update the command
        if (!builder.Resource.TryGetLastAnnotation<PythonEntrypointAnnotation>(out var entrypointAnnotation))
        {
            throw new InvalidOperationException("Cannot update virtual environment: Python entrypoint annotation not found.");
        }

        // Update the command based on entrypoint type
        string command = entrypointAnnotation.Type switch
        {
            EntrypointType.Executable => virtualEnvironment.GetExecutable(entrypointAnnotation.Entrypoint),
            EntrypointType.Script or EntrypointType.Module => virtualEnvironment.GetExecutable("python"),
            _ => throw new InvalidOperationException($"Unsupported entrypoint type: {entrypointAnnotation.Type}")
        };

        builder.WithCommand(command);
        builder.WithPythonEnvironment(env =>
        {
            env.VirtualEnvironment = virtualEnvironment;
        });

        return builder;
    }

    /// <summary>
    /// Configures the entrypoint for the Python application.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="entrypointType">The type of entrypoint (Script, Module, or Executable).</param>
    /// <param name="entrypoint">The entrypoint value (script path, module name, or executable name).</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method allows you to change the entrypoint configuration of a Python application after it has been created.
    /// The command and arguments will be updated based on the specified entrypoint type:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Script</b>: Runs as <c>python &lt;scriptPath&gt;</c></description></item>
    /// <item><description><b>Module</b>: Runs as <c>python -m &lt;moduleName&gt;</c></description></item>
    /// <item><description><b>Executable</b>: Runs the executable directly from the virtual environment</description></item>
    /// </list>
    /// <para>
    /// <b>Important:</b> This method resets all command-line arguments. If you need to add arguments after changing
    /// the entrypoint, call <c>WithArgs</c> after this method.
    /// </para>
    /// </remarks>
    /// <example>
    /// Change a Python app from running a script to running a module:
    /// <code lang="csharp">
    /// var python = builder.AddPythonScript("api", "../python-api", "main.py")
    ///     .WithEntrypoint(EntrypointType.Module, "uvicorn")
    ///     .WithArgs("main:app", "--reload");
    /// </code>
    /// </example>
    public static IResourceBuilder<PythonAppResource> WithEntrypoint(
        this IResourceBuilder<PythonAppResource> builder, EntrypointType entrypointType, string entrypoint)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(entrypoint);

        // Get or create the virtual environment from the annotation
        if (!builder.Resource.TryGetLastAnnotation<PythonEnvironmentAnnotation>(out var pythonEnv) ||
            pythonEnv.VirtualEnvironment is null)
        {
            throw new InvalidOperationException("Cannot set entrypoint: Python environment annotation with virtual environment not found.");
        }

        var virtualEnvironment = pythonEnv.VirtualEnvironment;

        // Determine the new command based on entrypoint type
        var command = entrypointType switch
        {
            EntrypointType.Executable => virtualEnvironment.GetExecutable(entrypoint),
            EntrypointType.Script or EntrypointType.Module => virtualEnvironment.GetExecutable("python"),
            _ => throw new ArgumentOutOfRangeException(nameof(entrypointType), entrypointType, "Invalid entrypoint type.")
        };

        // Update the command inline
        builder.WithCommand(command);
        builder.WithAnnotation(new PythonEntrypointAnnotation
        {
            Type = entrypointType,
            Entrypoint = entrypoint
        },
        ResourceAnnotationMutationBehavior.Replace);

        builder.WithArgs(static context =>
        {
            if (!context.Resource.TryGetLastAnnotation<PythonEntrypointAnnotation>(out var existingAnnotation))
            {
                return;
            }

            // Clear existing args since we're replacing the entrypoint
            context.Args.Clear();

            var entrypointType = existingAnnotation.Type;
            var entrypoint = existingAnnotation.Entrypoint;

            // Add entrypoint-specific arguments
            switch (entrypointType)
            {
                case EntrypointType.Module:
                    context.Args.Add("-m");
                    context.Args.Add(entrypoint);
                    break;
                case EntrypointType.Script:
                    context.Args.Add(entrypoint);
                    break;
                case EntrypointType.Executable:
                    // Executable runs directly, no additional args needed for entrypoint
                    break;
            }
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
