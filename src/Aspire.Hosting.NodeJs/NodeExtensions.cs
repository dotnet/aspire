// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREDOCKERFILEBUILDER001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.NodeJs;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Node applications to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class NodeAppHostingExtension
{
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
    /// <param name="useHttps">When true use HTTPS for the endpoints, otherwise use HTTP.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <example>
    /// The following example creates a Vite app using npm as the package manager.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// builder.AddViteApp("frontend", "./frontend")
    ///        .WithNpmPackageManager();
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<ViteAppResource> AddViteApp(this IDistributedApplicationBuilder builder, [ResourceName] string name, string workingDirectory, bool useHttps = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(workingDirectory);

        workingDirectory = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, workingDirectory));
        var resource = new ViteAppResource(name, "node", workingDirectory);

        var resourceBuilder = builder.AddResource(resource)
            .WithNodeDefaults()
            .WithIconName("CodeJsRectangle")
            .WithArgs(c =>
            {
                if (resource.TryGetLastAnnotation<JavaScriptPackageManagerAnnotation>(out var packageManagerAnnotation))
                {
                    foreach (var arg in packageManagerAnnotation.RunCommandLineArgs)
                    {
                        c.Args.Add(arg);
                    }
                }
                c.Args.Add("dev");

                if (packageManagerAnnotation?.CommandSeparator is string separator)
                {
                    c.Args.Add(separator);
                }

                var targetEndpoint = resource.GetEndpoint("https");
                if (!targetEndpoint.Exists)
                {
                    targetEndpoint = resource.GetEndpoint("http");
                }

                c.Args.Add("--port");
                c.Args.Add(targetEndpoint.Property(EndpointProperty.TargetPort));
            });

        _ = useHttps
            ? resourceBuilder.WithHttpsEndpoint(env: "PORT")
            : resourceBuilder.WithHttpEndpoint(env: "PORT");

        return resourceBuilder
            .AddNpmPackageManagerAnnotation(useCI: false)
            .PublishAsDockerFile(c =>
            {
                // Only generate a Dockerfile if one doesn't already exist in the app directory
                if (File.Exists(Path.Combine(resource.WorkingDirectory, "Dockerfile")))
                {
                    return;
                }

                c.WithDockerfileBuilder(resource.WorkingDirectory, dockerfileContext =>
                {
                    if (c.Resource.TryGetLastAnnotation<JavaScriptPackageManagerAnnotation>(out var packageManagerAnnotation)
                        && packageManagerAnnotation.BuildCommandLineArgs is { Length: > 0 })
                    {
                        var nodeVersion = DetectNodeVersion(resource.WorkingDirectory);
                        var dockerBuilder = dockerfileContext.Builder
                            .From($"node:{nodeVersion}-slim")
                            .WorkDir("/app")
                            .Copy(".", ".");

                        if (packageManagerAnnotation.InstallCommandLineArgs is { Length: > 0 })
                        {
                            dockerBuilder
                                .Run($"{resourceBuilder.Resource.Command} {string.Join(' ', packageManagerAnnotation.InstallCommandLineArgs)}");
                        }
                        dockerBuilder
                                .Run($"{resourceBuilder.Resource.Command} {string.Join(' ', packageManagerAnnotation.BuildCommandLineArgs)}");
                    }
                });
            });
    }

    /// <summary>
    /// Ensures the Node.js packages are installed before the application starts using npm as the package manager.
    /// </summary>
    /// <param name="resource">The NodeAppResource.</param>
    /// <param name="useCI">When true, use <code>npm ci</code>, otherwise use <code>npm install</code> when installing packages.</param>
    /// <param name="configureInstaller">Configure the npm installer resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<TResource> WithNpmPackageManager<TResource>(this IResourceBuilder<TResource> resource, bool useCI = false, Action<IResourceBuilder<NodeInstallerResource>>? configureInstaller = null) where TResource : NodeAppResource
    {
        AddNpmPackageManagerAnnotation(resource, useCI);

        // Only install packages during development, not in publish mode
        if (!resource.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            var installerName = $"{resource.Resource.Name}-npm-install";
            var installer = new NodeInstallerResource(installerName, resource.Resource.WorkingDirectory);

            var installerBuilder = resource.ApplicationBuilder.AddResource(installer)
                .WithCommand("npm")
                .WithArgs([useCI ? "ci" : "install"])
                .WithParentRelationship(resource.Resource)
                .ExcludeFromManifest();

            // Make the parent resource wait for the installer to complete
            resource.WaitForCompletion(installerBuilder);

            configureInstaller?.Invoke(installerBuilder);

            resource.WithAnnotation(new JavaScriptPackageInstallerAnnotation(installer));
        }

        return resource;
    }

    private static IResourceBuilder<TResource> AddNpmPackageManagerAnnotation<TResource>(this IResourceBuilder<TResource> resource, bool useCI) where TResource : NodeAppResource
    {
        resource.WithCommand("npm");
        resource.WithAnnotation(new JavaScriptPackageManagerAnnotation("npm")
        {
            InstallCommandLineArgs = [useCI ? "ci" : "install"],
            RunCommandLineArgs = ["run"],
            BuildCommandLineArgs = ["run", "build"]
        });

        return resource;
    }

    /// <summary>
    /// Detects the Node.js version to use for a project by checking common configuration files.
    /// </summary>
    /// <param name="workingDirectory">The working directory of the Node.js project.</param>
    /// <returns>The detected Node.js major version number as a string, or "22" as the default if no version is detected.</returns>
    private static string DetectNodeVersion(string workingDirectory)
    {
        // Check .nvmrc file
        var nvmrcPath = Path.Combine(workingDirectory, ".nvmrc");
        if (File.Exists(nvmrcPath))
        {
            var versionString = File.ReadAllText(nvmrcPath).Trim();
            if (TryParseNodeVersion(versionString, out var version))
            {
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
                return version;
            }
        }

        // Check package.json for engines.node
        var packageJsonPath = Path.Combine(workingDirectory, "package.json");
        if (File.Exists(packageJsonPath))
        {
            try
            {
                var packageJson = System.Text.Json.JsonDocument.Parse(File.ReadAllText(packageJsonPath));
                if (packageJson.RootElement.TryGetProperty("engines", out var engines) &&
                    engines.TryGetProperty("node", out var nodeVersion))
                {
                    var versionString = nodeVersion.GetString();
                    if (!string.IsNullOrWhiteSpace(versionString) && TryParseNodeVersion(versionString, out var version))
                    {
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
                        return version;
                    }
                }
            }
        }

        // Default to version 22
        return "22";
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

        // Remove common prefixes and operators
        var cleanedVersion = versionString
            .Trim()
            .TrimStart('v', 'V', '=', '~', '^', '>', '<', ' ')
            .Split('.', '-', ' ')[0]; // Take only the major version part

        // Try to parse as integer
        if (int.TryParse(cleanedVersion, out var majorVersionNumber) && majorVersionNumber > 0)
        {
            majorVersion = majorVersionNumber.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        return false;
    }
}
