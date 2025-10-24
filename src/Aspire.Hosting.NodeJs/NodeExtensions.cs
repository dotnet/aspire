// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREDOCKERFILEBUILDER001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Globalization;
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
    /// Adds a node application to the application model. Node should available on the PATH.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resource to.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="scriptPath">The path to the script that Node will execute.</param>
    /// <param name="workingDirectory">The working directory to use for the command. If null, the working directory of the current process is used.</param>
    /// <param name="args">The arguments to pass to the command.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
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
    /// Adds a node application to the application model. Executes the npm command with the specified script name.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resource to.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="workingDirectory">The working directory to use for the command. If null, the working directory of the current process is used.</param>
    /// <param name="scriptName">The npm script to execute. Defaults to "start".</param>
    /// <param name="args">The arguments to pass to the command.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
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

    private static IResourceBuilder<TResource> WithNodeDefaults<TResource>(this IResourceBuilder<TResource> builder) where TResource : NodeAppResource =>
        builder.WithOtlpExporter()
            .WithEnvironment("NODE_ENV", builder.ApplicationBuilder.Environment.IsDevelopment() ? "development" : "production")
            .WithExecutableCertificateTrustCallback((ctx) =>
            {
                if (ctx.Scope == CertificateTrustScope.Append)
                {
                    ctx.CertificateBundleEnvironment.Add("NODE_EXTRA_CA_CERTS");
                }
                else
                {
                    ctx.CertificateTrustArguments.Add("--use-openssl-ca");
                    ctx.CertificateBundleEnvironment.Add("SSL_CERT_FILE");
                }

                return Task.CompletedTask;
            });

    /// <summary>
    /// Adds a Vite app to the distributed application builder.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resource to.</param>
    /// <param name="name">The name of the Vite app.</param>
    /// <param name="workingDirectory">The working directory of the Vite app.</param>
    /// <param name="scriptName">The name of the script that runs the Vite app. Defaults to "dev".</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <example>
    /// The following example creates a Vite app using npm as the package manager.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// builder.AddViteApp("frontend", "./frontend")
    ///        .WithNpm(install: true);
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<ViteAppResource> AddViteApp(this IDistributedApplicationBuilder builder, [ResourceName] string name, string workingDirectory, string scriptName = "dev")
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(workingDirectory);

        workingDirectory = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, workingDirectory));
        var resource = new ViteAppResource(name, "node", workingDirectory);

        return builder.AddResource(resource)
            .WithNodeDefaults()
            .WithIconName("CodeJsRectangle")
            .WithArgs(c =>
            {
                if (resource.TryGetLastAnnotation<JavaScriptRunCommandAnnotation>(out var packageManagerAnnotation))
                {
                    foreach (var arg in packageManagerAnnotation.Args)
                    {
                        c.Args.Add(arg);
                    }
                }
                c.Args.Add(scriptName);
                c.Args.Add("--");

                var targetEndpoint = resource.GetEndpoint("https");
                if (!targetEndpoint.Exists)
                {
                    targetEndpoint = resource.GetEndpoint("http");
                }

                c.Args.Add("--port");
                c.Args.Add(targetEndpoint.Property(EndpointProperty.TargetPort));
            })
            .WithHttpEndpoint(env: "PORT")
            .WithNpm(install: false)
            .PublishAsDockerFile(c =>
            {
                // Only generate a Dockerfile if one doesn't already exist in the app directory
                if (File.Exists(Path.Combine(resource.WorkingDirectory, "Dockerfile")))
                {
                    return;
                }

                c.WithDockerfileBuilder(resource.WorkingDirectory, dockerfileContext =>
                {
                    if (c.Resource.TryGetLastAnnotation<JavaScriptBuildCommandAnnotation>(out var buildCommand))
                    {
                        var logger = dockerfileContext.Services.GetService<ILogger<ViteAppResource>>() ?? NullLogger<ViteAppResource>.Instance;
                        var nodeVersion = DetectNodeVersion(resource.WorkingDirectory, logger) ?? DefaultNodeVersion;
                        var dockerBuilder = dockerfileContext.Builder
                            .From($"node:{nodeVersion}-slim")
                            .WorkDir("/app")
                            .Copy(".", ".");

                        if (c.Resource.TryGetLastAnnotation<JavaScriptInstallCommandAnnotation>(out var installCommand))
                        {
                            dockerBuilder.Run($"{installCommand.Command} {string.Join(' ', installCommand.Args)}");
                        }

                        dockerBuilder.Run($"{buildCommand.Command} {string.Join(' ', buildCommand.Args)}");
                    }
                });

                // since Vite apps are typically served via a separate web server, we don't have an entrypoint
                if (resource.TryGetLastAnnotation<DockerfileBuildAnnotation>(out var dockerFileAnnotation))
                {
                    dockerFileAnnotation.HasEntrypoint = false;
                }
                else
                {
                    throw new InvalidOperationException("DockerfileBuildAnnotation should exist after calling PublishAsDockerFile.");
                }
            })
            .WithAnnotation(new ContainerFilesSourceAnnotation() { SourcePath = "/app/dist" });
    }

    /// <summary>
    /// Configures the Node.js resource to use npm as the package manager and optionally installs packages before the application starts.
    /// </summary>
    /// <param name="resource">The NodeAppResource.</param>
    /// <param name="install">When true, automatically installs packages before the application starts. When false (default), only sets the package manager annotation without creating an installer resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<TResource> WithNpm<TResource>(this IResourceBuilder<TResource> resource, bool install = false) where TResource : NodeAppResource
    {
        resource.WithCommand("npm")
            .WithAnnotation(new JavaScriptInstallCommandAnnotation("npm", ["install"]))
            .WithAnnotation(new JavaScriptRunCommandAnnotation(["run"]))
            .WithAnnotation(new JavaScriptBuildCommandAnnotation("npm", ["run", "build"]));

        AddInstaller(resource, install);
        return resource;
    }

    /// <summary>
    /// Configures the Node.js resource to use yarn as the package manager and optionally installs packages before the application starts.
    /// </summary>
    /// <param name="resource">The NodeAppResource.</param>
    /// <param name="install">When true, automatically installs packages before the application starts. When false (default), only sets the package manager annotation without creating an installer resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<TResource> WithYarn<TResource>(this IResourceBuilder<TResource> resource, bool install = false) where TResource : NodeAppResource
    {
        resource.WithCommand("yarn")
            .WithAnnotation(new JavaScriptInstallCommandAnnotation("yarn", ["install"]))
            .WithAnnotation(new JavaScriptRunCommandAnnotation(["run"]))
            .WithAnnotation(new JavaScriptBuildCommandAnnotation("yarn", ["run", "build"]));

        AddInstaller(resource, install);
        return resource;
    }

    /// <summary>
    /// Configures the Node.js resource to use pnmp as the package manager and optionally installs packages before the application starts.
    /// </summary>
    /// <param name="resource">The NodeAppResource.</param>
    /// <param name="install">When true, automatically installs packages before the application starts. When false (default), only sets the package manager annotation without creating an installer resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<TResource> WithPnpm<TResource>(this IResourceBuilder<TResource> resource, bool install = false) where TResource : NodeAppResource
    {
        resource.WithCommand("pnpm")
            .WithAnnotation(new JavaScriptInstallCommandAnnotation("pnpm", ["install"]))
            .WithAnnotation(new JavaScriptRunCommandAnnotation(["run"]))
            .WithAnnotation(new JavaScriptBuildCommandAnnotation("pnpm", ["run", "build"]));

        AddInstaller(resource, install);
        return resource;
    }

    /// <summary>
    /// Configures the Node.js resource to run the command to install packages before the application starts.
    /// </summary>
    /// <param name="resource">The NodeAppResource.</param>
    /// <param name="command">The executable command name</param>
    /// <param name="args">The command line arguments for the JavaScript package manager's install command.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<TResource> WithInstallCommand<TResource>(this IResourceBuilder<TResource> resource, string command, string[] args) where TResource : NodeAppResource
    {
        resource.WithAnnotation(new JavaScriptInstallCommandAnnotation(command, args));

        AddInstaller(resource, install: true);
        return resource;
    }

    /// <summary>
    /// Configures the Node.js resource to run the command to build the app during deployment.
    /// </summary>
    /// <param name="resource">The NodeAppResource.</param>
    /// <param name="command">The executable command name</param>
    /// <param name="args">The command line arguments for the JavaScript package manager's build command.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<TResource> WithBuildCommand<TResource>(this IResourceBuilder<TResource> resource, string command, string[] args) where TResource : NodeAppResource
    {
        return resource.WithAnnotation(new JavaScriptBuildCommandAnnotation(command, args));
    }

    private static void AddInstaller<TResource>(IResourceBuilder<TResource> resource, bool install) where TResource : NodeAppResource
    {
        // Only install packages if not in publish mode
        if (!resource.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            // Check if the installer resource already exists
            var installerName = $"{resource.Resource.Name}-installer";
            var existingResource = resource.ApplicationBuilder.Resources
                .FirstOrDefault(r => string.Equals(r.Name, installerName, StringComparison.OrdinalIgnoreCase));

            if (!install)
            {
                if (existingResource != null)
                {
                    // Remove existing installer resource if install is false
                    resource.ApplicationBuilder.Resources.Remove(existingResource);
                    resource.Resource.Annotations.OfType<WaitAnnotation>()
                        .Where(w => w.Resource == existingResource)
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

            var installer = new NodeInstallerResource(installerName, resource.Resource.WorkingDirectory);
            var installerBuilder = resource.ApplicationBuilder.AddResource(installer)
                .WithParentRelationship(resource.Resource)
                .ExcludeFromManifest();

            resource.ApplicationBuilder.Eventing.Subscribe<BeforeStartEvent>((e, _) =>
            {
                // set the installer's working directory to match the resource's working directory
                // and set the install command and args based on the resource's annotations
                if (!resource.Resource.TryGetLastAnnotation<JavaScriptInstallCommandAnnotation>(out var installCommand))
                {
                    throw new InvalidOperationException("JavaScriptInstallCommandAnnotation is required when installing packages.");
                }

                installerBuilder
                    .WithCommand(installCommand.Command)
                    .WithWorkingDirectory(resource.Resource.WorkingDirectory)
                    .WithArgs(installCommand.Args);

                return Task.CompletedTask;
            });

            // Make the parent resource wait for the installer to complete
            resource.WaitForCompletion(installerBuilder);

            resource.WithAnnotation(new JavaScriptPackageInstallerAnnotation(installer));
        }
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
