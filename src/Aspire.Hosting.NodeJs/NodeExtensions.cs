// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREDOCKERFILEBUILDER001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.NodeJs;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Node applications to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class NodeAppHostingExtension
{
    private const string DefaultNodeVersion = "22";

    /// <summary>
    /// Adds a node application to the application model. Node should be available on the PATH.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resource to.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="scriptPath">The path to the script that Node will execute.</param>
    /// <param name="workingDirectory">The working directory to use for the command. If null, the directory of the <paramref name="scriptPath"/> is used.</param>
    /// <param name="args">The arguments to pass to the command.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    [Obsolete("Use AddNodeApp that takes an appDirectory and relative scriptPath instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IResourceBuilder<NodeAppResource> AddNodeApp(this IDistributedApplicationBuilder builder, [ResourceName] string name, string scriptPath, string? workingDirectory = null, string[]? args = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(scriptPath);

        args ??= [];
        string[] effectiveArgs = [scriptPath, .. args];
        workingDirectory ??= Path.GetDirectoryName(scriptPath)!;
        workingDirectory = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, workingDirectory));

        var resource = new NodeAppResource(name, "node", workingDirectory);

        return builder.AddResource(resource)
                      .WithNodeDefaults()
                      .WithArgs(effectiveArgs)
                      .WithIconName("CodeJsRectangle");
    }

    /// <summary>
    /// Adds a node application to the application model. Node should be available on the PATH.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resource to.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="appDirectory">The path to the directory containing the node application.</param>
    /// <param name="scriptPath">The path to the script relative to the app directory to run.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// This method executes a Node script directly using <c>node script.js</c>. If you want to use a package manager
    /// you can add one and configure the install and run scripts using the provided extension methods.
    ///
    /// If the application directory contains a <c>package.json</c> file, npm will be added as the default package manager.
    /// </remarks>
    /// <example>
    /// Add a Node app to the application model using yarn and 'yarn run dev' for running during development:
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// builder.AddNodeApp("frontend", "../frontend", "app.js")
    ///        .WithYarn()
    ///        .WithRunScript("dev");
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    [OverloadResolutionPriority(1)]
    public static IResourceBuilder<NodeAppResource> AddNodeApp(this IDistributedApplicationBuilder builder, [ResourceName] string name, string appDirectory, string scriptPath)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(scriptPath);

        appDirectory = Path.GetFullPath(appDirectory, builder.AppHostDirectory);
        var resource = new NodeAppResource(name, "node", appDirectory);

        var resourceBuilder = builder.AddResource(resource)
            .WithNodeDefaults()
            .WithArgs(c =>
            {
                // If the JavaScriptRunScriptAnnotation is present, use that to run the app
                if (c.Resource.TryGetLastAnnotation<JavaScriptRunScriptAnnotation>(out var runCommand) &&
                    c.Resource.TryGetLastAnnotation<JavaScriptPackageManagerAnnotation>(out var packageManager))
                {
                    if (!string.IsNullOrEmpty(packageManager.ScriptCommand))
                    {
                        c.Args.Add(packageManager.ScriptCommand);
                    }

                    c.Args.Add(runCommand.ScriptName);

                    foreach (var arg in runCommand.Args)
                    {
                        c.Args.Add(arg);
                    }
                }
                else
                {
                    c.Args.Add(scriptPath);
                }
            })
            .WithIconName("CodeJsRectangle")
            .PublishAsDockerFile(c =>
            {
                // Only generate a Dockerfile if one doesn't already exist in the app directory
                if (File.Exists(Path.Combine(resource.WorkingDirectory, "Dockerfile")))
                {
                    return;
                }

                c.WithDockerfileBuilder(resource.WorkingDirectory, dockerfileContext =>
                {
                    var defaultBaseImage = new Lazy<string>(() => GetDefaultBaseImage(appDirectory, "alpine", dockerfileContext.Services));

                    // Get custom base image from annotation, if present
                    dockerfileContext.Resource.TryGetLastAnnotation<DockerfileBaseImageAnnotation>(out var baseImageAnnotation);

                    var baseBuildImage = baseImageAnnotation?.BuildImage ?? defaultBaseImage.Value;
                    var builderStage = dockerfileContext.Builder
                        .From(baseBuildImage, "build")
                        .EmptyLine()
                        .WorkDir("/app")
                        .Copy(".", ".")
                        .EmptyLine();

                    if (resource.TryGetLastAnnotation<JavaScriptPackageManagerAnnotation>(out var packageManager))
                    {
                        if (resource.TryGetLastAnnotation<JavaScriptInstallCommandAnnotation>(out var installCommand))
                        {
                            builderStage.Run($"{packageManager.ExecutableName} {string.Join(' ', installCommand.Args)}");
                        }

                        if (resource.TryGetLastAnnotation<JavaScriptBuildScriptAnnotation>(out var buildCommand))
                        {
                            var commandArgs = new List<string>() { packageManager.ExecutableName };
                            if (!string.IsNullOrEmpty(packageManager.ScriptCommand))
                            {
                                commandArgs.Add(packageManager.ScriptCommand);
                            }
                            commandArgs.Add(buildCommand.ScriptName);
                            commandArgs.AddRange(buildCommand.Args);

                            builderStage.Run(string.Join(' ', commandArgs));
                        }
                    }

                    var baseRuntimeImage = baseImageAnnotation?.RuntimeImage ?? defaultBaseImage.Value;
                    var runtimeBuilder = dockerfileContext.Builder
                        .From(baseRuntimeImage, "runtime")
                            .EmptyLine()
                            .WorkDir("/app")
                            .CopyFrom("build", "/app", "/app")
                            .EmptyLine()
                            .Env("NODE_ENV", "production")
                            .Expose(3000)
                            .EmptyLine()
                            .User("node")
                            .EmptyLine()
                            .Entrypoint([resource.Command, scriptPath]);
                });
            });

        if (File.Exists(Path.Combine(appDirectory, "package.json")))
        {
            // Automatically add npm as the package manager if a package.json file exists
            resourceBuilder.WithNpm();
        }

        if (builder.ExecutionContext.IsRunMode)
        {
            builder.Eventing.Subscribe<BeforeStartEvent>((_, _) =>
            {
                // set the command to the package manager executable if the JavaScriptRunScriptAnnotation is present
                if (resourceBuilder.Resource.TryGetLastAnnotation<JavaScriptRunScriptAnnotation>(out _) &&
                    resourceBuilder.Resource.TryGetLastAnnotation<JavaScriptPackageManagerAnnotation>(out var packageManager))
                {
                    resourceBuilder.WithCommand(packageManager.ExecutableName);
                }

                return Task.CompletedTask;
            });
        }

        return resourceBuilder;
    }

    /// <summary>
    /// Adds a node application to the application model. Executes the npm command with the specified script name.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resource to.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="workingDirectory">The working directory to use for the command.</param>
    /// <param name="scriptName">The npm script to execute. Defaults to "start".</param>
    /// <param name="args">The arguments to pass to the command.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    [Obsolete("Use AddJavaScriptApp instead.")]
    public static IResourceBuilder<NodeAppResource> AddNpmApp(this IDistributedApplicationBuilder builder, [ResourceName] string name, string workingDirectory, string scriptName = "start", string[]? args = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(workingDirectory);
        ArgumentException.ThrowIfNullOrEmpty(scriptName);

        string[] allArgs = args is { Length: > 0 }
            ? ["run", scriptName, "--", .. args]
            : ["run", scriptName];

        workingDirectory = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, workingDirectory));
        var resource = new NodeAppResource(name, "npm", workingDirectory);

        return builder.AddResource(resource)
                      .WithNodeDefaults()
                      .WithArgs(allArgs)
                      .WithIconName("CodeJsRectangle");
    }

    private static IResourceBuilder<TResource> WithNodeDefaults<TResource>(this IResourceBuilder<TResource> builder) where TResource : JavaScriptAppResource =>
        builder.WithOtlpExporter()
            .WithEnvironment("NODE_ENV", builder.ApplicationBuilder.Environment.IsDevelopment() ? "development" : "production")
            .WithCertificateTrustConfiguration((ctx) =>
            {
                if (ctx.Scope == CertificateTrustScope.Append)
                {
                    ctx.EnvironmentVariables["NODE_EXTRA_CA_CERTS"] = ctx.CertificateBundlePath;
                }
                else
                {
                    ctx.Arguments.Add("--use-openssl-ca");
                }

                return Task.CompletedTask;
            });

    /// <summary>
    /// Adds a JavaScript application resource to the distributed application using the specified app directory and
    /// run script.
    /// </summary>
    /// <param name="builder">The distributed application builder to which the JavaScript application resource will be added.</param>
    /// <param name="name">The unique name of the JavaScript application resource. Cannot be null or empty.</param>
    /// <param name="appDirectory">The path to the directory containing the JavaScript application.</param>
    /// <param name="runScriptName">The name of the npm script to run when starting the application. Defaults to "dev". Cannot be null or empty.</param>
    /// <returns>A resource builder for the newly added JavaScript application resource.</returns>
    /// <remarks>
    /// If a Dockerfile does not exist in the application's directory, one will be generated
    /// automatically when publishing. The method configures the resource with Node.js defaults and sets up npm
    /// integration.
    /// </remarks>
    public static IResourceBuilder<JavaScriptAppResource> AddJavaScriptApp(this IDistributedApplicationBuilder builder, [ResourceName] string name, string appDirectory, string runScriptName = "dev")
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(appDirectory);
        ArgumentException.ThrowIfNullOrEmpty(runScriptName);

        appDirectory = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, appDirectory));
        var resource = new JavaScriptAppResource(name, "npm", appDirectory);

        return builder.CreateDefaultJavaScriptAppBuilder(resource, appDirectory, runScriptName);
    }

    private static IResourceBuilder<TResource> CreateDefaultJavaScriptAppBuilder<TResource>(
        this IDistributedApplicationBuilder builder,
        TResource resource,
        string appDirectory,
        string runScriptName,
        Action<CommandLineArgsCallbackContext>? argsCallback = null) where TResource : JavaScriptAppResource
    {
        var resourceBuilder = builder.AddResource(resource)
            .WithNodeDefaults()
            .WithArgs(c =>
            {
                if (c.Resource.TryGetLastAnnotation<JavaScriptRunScriptAnnotation>(out var runCommand))
                {
                    if (c.Resource.TryGetLastAnnotation<JavaScriptPackageManagerAnnotation>(out var packageManager) &&
                        !string.IsNullOrEmpty(packageManager.ScriptCommand))
                    {
                        c.Args.Add(packageManager.ScriptCommand);
                    }

                    c.Args.Add(runCommand.ScriptName);

                    foreach (var arg in runCommand.Args)
                    {
                        c.Args.Add(arg);
                    }
                }

                argsCallback?.Invoke(c);
            })
            .WithIconName("CodeJsRectangle")
            .WithNpm()
            .PublishAsDockerFile(c =>
            {
                // Only generate a Dockerfile if one doesn't already exist in the app directory
                if (File.Exists(Path.Combine(appDirectory, "Dockerfile")))
                {
                    return;
                }

                c.WithDockerfileBuilder(appDirectory, dockerfileContext =>
                {
                    if (c.Resource.TryGetLastAnnotation<JavaScriptPackageManagerAnnotation>(out var packageManager))
                    {
                        // Get custom base image from annotation, if present
                        dockerfileContext.Resource.TryGetLastAnnotation<DockerfileBaseImageAnnotation>(out var baseImageAnnotation);
                        var baseImage = baseImageAnnotation?.BuildImage ?? GetDefaultBaseImage(appDirectory, "slim", dockerfileContext.Services);

                        var dockerBuilder = dockerfileContext.Builder
                            .From(baseImage)
                            .WorkDir("/app")
                            .Copy(".", ".");

                        if (c.Resource.TryGetLastAnnotation<JavaScriptInstallCommandAnnotation>(out var installCommand))
                        {
                            dockerBuilder.Run($"{packageManager.ExecutableName} {string.Join(' ', installCommand.Args)}");
                        }

                        if (c.Resource.TryGetLastAnnotation<JavaScriptBuildScriptAnnotation>(out var buildCommand))
                        {
                            var commandArgs = new List<string>() { packageManager.ExecutableName };
                            if (!string.IsNullOrEmpty(packageManager.ScriptCommand))
                            {
                                commandArgs.Add(packageManager.ScriptCommand);
                            }
                            commandArgs.Add(buildCommand.ScriptName);
                            commandArgs.AddRange(buildCommand.Args);

                            dockerBuilder.Run(string.Join(' ', commandArgs));
                        }
                    }
                });

                // Javascript apps don't have an entrypoint
                if (resource.TryGetLastAnnotation<DockerfileBuildAnnotation>(out var dockerFileAnnotation))
                {
                    dockerFileAnnotation.HasEntrypoint = false;
                }
                else
                {
                    throw new InvalidOperationException("DockerfileBuildAnnotation should exist after calling PublishAsDockerFile.");
                }
            })
            .WithAnnotation(new ContainerFilesSourceAnnotation() { SourcePath = "/app/dist" })
            .WithBuildScript("build")
            .WithRunScript(runScriptName);

        // ensure the package manager command is set before starting the resource
        if (builder.ExecutionContext.IsRunMode)
        {
            builder.Eventing.Subscribe<BeforeStartEvent>((_, _) =>
            {
                if (resourceBuilder.Resource.TryGetLastAnnotation<JavaScriptPackageManagerAnnotation>(out var packageManager))
                {
                    resourceBuilder.WithCommand(packageManager.ExecutableName);
                }

                return Task.CompletedTask;
            });
        }

        return resourceBuilder;
    }

    /// <summary>
    /// Adds a Vite app to the distributed application builder.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resource to.</param>
    /// <param name="name">The name of the Vite app.</param>
    /// <param name="appDirectory">The path to the directory containing the Vite app.</param>
    /// <param name="runScriptName">The name of the script that runs the Vite app. Defaults to "dev".</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <example>
    /// The following example creates a Vite app using npm as the package manager.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// builder.AddViteApp("frontend", "./frontend");
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<ViteAppResource> AddViteApp(this IDistributedApplicationBuilder builder, [ResourceName] string name, string appDirectory, string runScriptName = "dev")
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(appDirectory);

        appDirectory = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, appDirectory));
        var resource = new ViteAppResource(name, "npm", appDirectory);

        return builder.CreateDefaultJavaScriptAppBuilder(
            resource,
            appDirectory,
            runScriptName,
            argsCallback: c =>
            {
                c.Args.Add("--");

                var targetEndpoint = resource.GetEndpoint("https");
                if (!targetEndpoint.Exists)
                {
                    targetEndpoint = resource.GetEndpoint("http");
                }

                c.Args.Add("--port");
                c.Args.Add(targetEndpoint.Property(EndpointProperty.TargetPort));
            })
            .WithHttpEndpoint(env: "PORT");
    }

    /// <summary>
    /// Configures the Node.js resource to use npm as the package manager and optionally installs packages before the application starts.
    /// </summary>
    /// <param name="resource">The NodeAppResource.</param>
    /// <param name="install">When true (default), automatically installs packages before the application starts. When false, only sets the package manager annotation without creating an installer resource.</param>
    /// <param name="installCommand">The install command itself passed to npm to install dependencies.</param>
    /// <param name="installArgs">The command-line arguments passed to npm to install dependencies.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<TResource> WithNpm<TResource>(this IResourceBuilder<TResource> resource, bool install = true, string? installCommand = null, string[]? installArgs = null) where TResource : JavaScriptAppResource
    {
        ArgumentNullException.ThrowIfNull(resource);

        installCommand ??= GetDefaultNpmInstallCommand(resource);

        resource
            .WithAnnotation(new JavaScriptPackageManagerAnnotation("npm", runScriptCommand: "run"))
            .WithAnnotation(new JavaScriptInstallCommandAnnotation([installCommand, .. installArgs ?? []]));

        AddInstaller(resource, install);
        return resource;
    }

    private static string GetDefaultNpmInstallCommand(IResourceBuilder<JavaScriptAppResource> resource) =>
        resource.ApplicationBuilder.ExecutionContext.IsPublishMode &&
            File.Exists(Path.Combine(resource.Resource.WorkingDirectory, "package-lock.json"))
            ? "ci"
            : "install";

    /// <summary>
    /// Configures the Node.js resource to use yarn as the package manager and optionally installs packages before the application starts.
    /// </summary>
    /// <param name="resource">The NodeAppResource.</param>
    /// <param name="install">When true (default), automatically installs packages before the application starts. When false, only sets the package manager annotation without creating an installer resource.</param>
    /// <param name="installArgs">The command-line arguments passed to "yarn install".</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<TResource> WithYarn<TResource>(this IResourceBuilder<TResource> resource, bool install = true, string[]? installArgs = null) where TResource : JavaScriptAppResource
    {
        ArgumentNullException.ThrowIfNull(resource);

        installArgs ??= GetDefaultYarnInstallArgs(resource);

        resource
            .WithAnnotation(new JavaScriptPackageManagerAnnotation("yarn", runScriptCommand: "run"))
            .WithAnnotation(new JavaScriptInstallCommandAnnotation(["install", .. installArgs]));

        AddInstaller(resource, install);
        return resource;
    }

    private static string[] GetDefaultYarnInstallArgs(IResourceBuilder<JavaScriptAppResource> resource)
    {
        var workingDirectory = resource.Resource.WorkingDirectory;
        if (!resource.ApplicationBuilder.ExecutionContext.IsPublishMode ||
            !File.Exists(Path.Combine(workingDirectory, "yarn.lock")))
        {
            // Not publish mode or no yarn.lock, use default install args
            return [];
        }

        var yarnRcYml = Path.Combine(workingDirectory, ".yarnrc.yml");
        var yarnBerryReleaseDir = Path.Combine(workingDirectory, ".yarn", "releases");
        var hasYarnBerry = File.Exists(yarnRcYml) || Directory.Exists(yarnBerryReleaseDir);

        if (hasYarnBerry)
        {
            // Yarn 2+ detected
            return ["--immutable"];
        }

        // Fallback: default to Yarn v1.x behavior
        return ["--frozen-lockfile"];
    }

    /// <summary>
    /// Configures the Node.js resource to use pnmp as the package manager and optionally installs packages before the application starts.
    /// </summary>
    /// <param name="resource">The NodeAppResource.</param>
    /// <param name="install">When true (default), automatically installs packages before the application starts. When false, only sets the package manager annotation without creating an installer resource.</param>
    /// <param name="installArgs">The command-line arguments passed to "pnpm install".</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<TResource> WithPnpm<TResource>(this IResourceBuilder<TResource> resource, bool install = true, string[]? installArgs = null) where TResource : JavaScriptAppResource
    {
        ArgumentNullException.ThrowIfNull(resource);

        installArgs ??= GetDefaultPnpmInstallArgs(resource);

        resource
            .WithAnnotation(new JavaScriptPackageManagerAnnotation("pnpm", runScriptCommand: "run"))
            .WithAnnotation(new JavaScriptInstallCommandAnnotation(["install", .. installArgs]));

        AddInstaller(resource, install);
        return resource;
    }

    private static string[] GetDefaultPnpmInstallArgs(IResourceBuilder<JavaScriptAppResource> resource) =>
        resource.ApplicationBuilder.ExecutionContext.IsPublishMode &&
            File.Exists(Path.Combine(resource.Resource.WorkingDirectory, "pnpm-lock.yaml"))
            ? ["--frozen-lockfile"]
            : [];

    /// <summary>
    /// Adds a build script annotation to the resource builder using the specified command-line arguments.
    /// </summary>
    /// <typeparam name="TResource">The type of JavaScript application resource being configured.</typeparam>
    /// <param name="resource">The resource builder to which the build script annotation will be added.</param>
    /// <param name="scriptName">The name of the script to be executed when the resource is built.</param>
    /// <param name="args">An array of command-line arguments to use for the build script.</param>
    /// <returns>The same resource builder instance with the build script annotation applied.</returns>
    /// <remarks>
    /// Use this method to specify custom build scripts for JavaScript application resources during
    /// deployment.
    /// </remarks>
    public static IResourceBuilder<TResource> WithBuildScript<TResource>(this IResourceBuilder<TResource> resource, string scriptName, string[]? args = null) where TResource : JavaScriptAppResource
    {
        return resource.WithAnnotation(new JavaScriptBuildScriptAnnotation(scriptName, args));
    }

    /// <summary>
    /// Adds a run script annotation to the specified JavaScript application resource builder, specifying the script to
    /// execute and its arguments during run mode.
    /// </summary>
    /// <typeparam name="TResource">The type of the JavaScript application resource being configured. Must inherit from JavaScriptAppResource.</typeparam>
    /// <param name="resource">The resource builder to which the run script annotation will be added.</param>
    /// <param name="scriptName">The name of the script to be executed when the resource is run.</param>
    /// <param name="args">An array of arguments to pass to the script.</param>
    /// <returns>The same resource builder instance with the run script annotation applied, enabling further configuration.</returns>
    /// <remarks>
    /// Use this method to specify a custom script and its arguments that should be executed when the resource is executed
    /// in RunMode.
    /// </remarks>
    public static IResourceBuilder<TResource> WithRunScript<TResource>(this IResourceBuilder<TResource> resource, string scriptName, string[]? args = null) where TResource : JavaScriptAppResource
    {
        return resource.WithAnnotation(new JavaScriptRunScriptAnnotation(scriptName, args));
    }

    private static void AddInstaller<TResource>(IResourceBuilder<TResource> resource, bool install) where TResource : JavaScriptAppResource
    {
        // Only install packages if in run mode
        if (resource.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            // Check if the installer resource already exists
            var installerName = $"{resource.Resource.Name}-installer";
            resource.ApplicationBuilder.TryCreateResourceBuilder<JavaScriptInstallerResource>(installerName, out var existingResource);

            if (!install)
            {
                if (existingResource != null)
                {
                    // Remove existing installer resource if install is false
                    resource.ApplicationBuilder.Resources.Remove(existingResource.Resource);
                    resource.Resource.Annotations.OfType<WaitAnnotation>()
                        .Where(w => w.Resource == existingResource.Resource)
                        .ToList()
                        .ForEach(w => resource.Resource.Annotations.Remove(w));
                    resource.Resource.Annotations.OfType<JavaScriptPackageInstallerAnnotation>()
                        .ToList()
                        .ForEach(a => resource.Resource.Annotations.Remove(a));
                }
                else
                {
                    // No installer needed
                }
                return;
            }

            if (existingResource is not null)
            {
                // Installer already exists
                return;
            }

            var installer = new JavaScriptInstallerResource(installerName, resource.Resource.WorkingDirectory);
            var installerBuilder = resource.ApplicationBuilder.AddResource(installer)
                .WithParentRelationship(resource.Resource)
                .ExcludeFromManifest();

            resource.ApplicationBuilder.Eventing.Subscribe<BeforeStartEvent>((_, _) =>
            {
                // set the installer's working directory to match the resource's working directory
                // and set the install command and args based on the resource's annotations
                if (!resource.Resource.TryGetLastAnnotation<JavaScriptPackageManagerAnnotation>(out var packageManager) ||
                    !resource.Resource.TryGetLastAnnotation<JavaScriptInstallCommandAnnotation>(out var installCommand))
                {
                    throw new InvalidOperationException("JavaScriptPackageManagerAnnotation and JavaScriptInstallCommandAnnotation are required when installing packages.");
                }

                installerBuilder
                    .WithCommand(packageManager.ExecutableName)
                    .WithWorkingDirectory(resource.Resource.WorkingDirectory)
                    .WithArgs(installCommand.Args);

                return Task.CompletedTask;
            });

            // Make the parent resource wait for the installer to complete
            resource.WaitForCompletion(installerBuilder);

            resource.WithAnnotation(new JavaScriptPackageInstallerAnnotation(installer));
        }
    }

    private static string GetDefaultBaseImage(string appDirectory, string defaultSuffix, IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetService<ILogger<JavaScriptAppResource>>() ?? NullLogger<JavaScriptAppResource>.Instance;
        var nodeVersion = DetectNodeVersion(appDirectory, logger) ?? DefaultNodeVersion;
        return $"node:{nodeVersion}-{defaultSuffix}";
    }

    /// <summary>
    /// Detects the Node.js version to use for a project by checking common configuration files.
    /// </summary>
    /// <param name="workingDirectory">The working directory of the Node.js project.</param>
    /// <param name="logger">The logger for diagnostic messages.</param>
    /// <returns>The detected Node.js major version number as a string, or <c>null</c> if no version is detected.</returns>
    private static string? DetectNodeVersion(string workingDirectory, ILogger logger)
    {
        // Check .nvmrc file
        var nvmrcPath = Path.Combine(workingDirectory, ".nvmrc");
        if (File.Exists(nvmrcPath))
        {
            var versionString = File.ReadAllText(nvmrcPath).Trim();
            if (TryParseNodeVersion(versionString, out var version))
            {
                logger.LogDebug("Detected Node.js version {Version} from .nvmrc file", version);
                return version;
            }
        }

        // Check .node-version file
        var nodeVersionPath = Path.Combine(workingDirectory, ".node-version");
        if (File.Exists(nodeVersionPath))
        {
            var versionString = File.ReadAllText(nodeVersionPath).Trim();
            if (TryParseNodeVersion(versionString, out var version))
            {
                logger.LogDebug("Detected Node.js version {Version} from .node-version file", version);
                return version;
            }
        }

        // Check package.json for engines.node
        var packageJsonPath = Path.Combine(workingDirectory, "package.json");
        if (File.Exists(packageJsonPath))
        {
            try
            {
                using var stream = File.OpenRead(packageJsonPath);
                using var packageJson = JsonDocument.Parse(stream);
                if (packageJson.RootElement.TryGetProperty("engines", out var engines) &&
                    engines.TryGetProperty("node", out var nodeVersion))
                {
                    var versionString = nodeVersion.GetString();
                    if (!string.IsNullOrWhiteSpace(versionString) && TryParseNodeVersion(versionString, out var version))
                    {
                        logger.LogDebug("Detected Node.js version {Version} from package.json engines.node field", version);
                        return version;
                    }
                }
            }
            catch
            {
                // If package.json parsing fails, continue to default
            }
        }

        // Check .tool-versions file (asdf)
        var toolVersionsPath = Path.Combine(workingDirectory, ".tool-versions");
        if (File.Exists(toolVersionsPath))
        {
            var lines = File.ReadAllLines(toolVersionsPath);
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("nodejs ", StringComparison.Ordinal) ||
                    trimmedLine.StartsWith("node ", StringComparison.Ordinal))
                {
                    var parts = trimmedLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1 && TryParseNodeVersion(parts[1], out var version))
                    {
                        logger.LogDebug("Detected Node.js version {Version} from .tool-versions file", version);
                        return version;
                    }
                }
            }
        }

        // Return null if no version is detected
        logger.LogDebug("No Node.js version detected, using default version {DefaultVersion}", DefaultNodeVersion);
        return null;
    }

    /// <summary>
    /// Attempts to parse a Node.js version string and extract the major version number.
    /// </summary>
    /// <param name="versionString">The version string to parse (e.g., "22", "v22.1.0", ">=20.12", "^18.0.0").</param>
    /// <param name="majorVersion">The extracted major version number as a string.</param>
    /// <returns>True if the version was successfully parsed, false otherwise.</returns>
    private static bool TryParseNodeVersion(string versionString, out string majorVersion)
    {
        majorVersion = string.Empty;

        if (string.IsNullOrWhiteSpace(versionString))
        {
            return false;
        }

        // Remove common prefixes and operators (handle multi-character operators first)
        var cleaned = versionString.Trim();
        string[] operators = [">=", "<=", "==", ">", "<", "=", "~", "^", "v", "V"];
        foreach (var op in operators)
        {
            if (cleaned.StartsWith(op, StringComparison.Ordinal))
            {
                cleaned = cleaned.Substring(op.Length).TrimStart();
                break;
            }
        }
        var cleanedVersion = cleaned.Split('.', '-', ' ')[0]; // Take only the major version part

        // Try to parse as integer
        if (int.TryParse(cleanedVersion, NumberStyles.None, CultureInfo.InvariantCulture, out var majorVersionNumber) && majorVersionNumber > 0)
        {
            majorVersion = majorVersionNumber.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        return false;
    }
}
