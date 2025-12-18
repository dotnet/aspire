// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Mcp;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding MCP bridge resources to the application model.
/// </summary>
public static class McpResourceBuilderExtensions
{
    /// <summary>
    /// Adds an MCP bridge resource that proxies a stdio-based MCP server to an HTTP endpoint.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="mcpServerCommand">The command to execute for the stdio MCP server.</param>
    /// <param name="mcpServerArgs">The arguments to pass to the stdio MCP server command.</param>
    /// <param name="mcpServerWorkingDirectory">The working directory for the stdio MCP server process.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <para>
    /// The MCP bridge spawns a stdio-based MCP server process and exposes it via an HTTP endpoint
    /// that the Aspire Dashboard can connect to. This enables stdio MCP servers (like npx-based servers)
    /// to integrate with the Dashboard's HTTP-only MCP proxy.
    /// </para>
    /// <example>
    /// Add an MCP bridge for an npx-based MCP server:
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// var mcpBridge = builder.AddMcpBridge("weather-mcp", "npx", ["-y", "@modelcontextprotocol/server-weather"]);
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<McpBridgeResource> AddMcpBridge(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        string mcpServerCommand,
        string[]? mcpServerArgs = null,
        string? mcpServerWorkingDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(mcpServerCommand);

        // Find the bridge executable path
        var bridgeExeName = OperatingSystem.IsWindows() ? "Aspire.Hosting.Mcp.Bridge.exe" : "Aspire.Hosting.Mcp.Bridge";
        var rid = GetCurrentRuntimeIdentifier();
        
        var bridgeExePath = FindBridgeExecutable(bridgeExeName, rid);
        var bridgeDir = Path.GetDirectoryName(bridgeExePath)!;

        var resource = new McpBridgeResource(name, bridgeExePath, bridgeDir)
        {
            McpServerCommand = mcpServerCommand,
            McpServerArguments = mcpServerArgs,
            McpServerWorkingDirectory = mcpServerWorkingDirectory
        };

        // Add HTTP endpoint for the bridge
        // The port is dynamically allocated and injected via HTTP_PORTS environment variable
        var resourceBuilder = builder.AddResource(resource)
            .WithHttpEndpoint(env: "PORT");

        // Add health check after endpoint is configured
        resourceBuilder.WithHttpHealthCheck("/health", endpointName: "http");

        // Configure environment variables for the stdio MCP server
        resourceBuilder
            .WithEnvironment("MCP_SERVER_COMMAND", mcpServerCommand)
            .WithEnvironment("MCP_SERVER_ARGS", mcpServerArgs != null ? string.Join(" ", mcpServerArgs) : string.Empty);

        if (!string.IsNullOrWhiteSpace(mcpServerWorkingDirectory))
        {
            resourceBuilder.WithEnvironment("MCP_SERVER_WORKING_DIRECTORY", mcpServerWorkingDirectory);
        }

        // Subscribe to endpoint allocation to register MCP endpoint
        builder.Eventing.Subscribe<ResourceEndpointsAllocatedEvent>(resource, async (@event, ct) =>
        {
            var mcpEndpoint = resource.GetEndpoint("http");
            if (mcpEndpoint.IsAllocated)
            {
                // Construct the full MCP endpoint URL by appending /mcp to the base URL
                var mcpUri = new Uri(new Uri(mcpEndpoint.Url), "mcp");

                // Register the MCP endpoint annotation such that the Dashboard can discover it
                resourceBuilder.WithMcpEndpoint(
                    new McpEndpointDefinition(
                        mcpUri,
                        "http",
                        resource.ApiKey,
                        resource.McpNamespace));

                var notificationService = @event.Services.GetRequiredService<ResourceNotificationService>();
                await notificationService.PublishUpdateAsync(resource, s => s).ConfigureAwait(false);
            }
        });

        return resourceBuilder;
    }

    /// <summary>
    /// Configures the working directory for the stdio MCP server process.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="workingDirectory">The working directory path.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<McpBridgeResource> WithWorkingDirectory(
        this IResourceBuilder<McpBridgeResource> builder,
        string workingDirectory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(workingDirectory);

        builder.Resource.McpServerWorkingDirectory = workingDirectory;
        builder.WithEnvironment("MCP_SERVER_WORKING_DIRECTORY", workingDirectory);

        return builder;
    }

    /// <summary>
    /// Adds environment variables that will be passed to the stdio MCP server process.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the environment variable.</param>
    /// <param name="value">The value of the environment variable.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<McpBridgeResource> WithProcessEnvironment(
        this IResourceBuilder<McpBridgeResource> builder,
        string name,
        string value)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        // Add the environment variable with a prefix so it can be passed to the child process
        builder.WithEnvironment($"MCP_PROC_ENV_{name}", value);

        return builder;
    }

    /// <summary>
    /// Adds environment variables that will be passed to the stdio MCP server process.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the environment variable.</param>
    /// <param name="value">A reference expression that will be resolved at runtime.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<McpBridgeResource> WithProcessEnvironment(
        this IResourceBuilder<McpBridgeResource> builder,
        string name,
        ReferenceExpression value)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(value);

        // Add the environment variable with a prefix so it can be passed to the child process
        // Use the callback so it waits for BeforeStartEvent in case the expressions are resolved later
        builder.WithEnvironment(ctx => { ctx.EnvironmentVariables[$"MCP_PROC_ENV_{name}"] = value; });

        return builder;
    }

    /// <summary>
    /// Adds a command-line argument that will be passed to the stdio MCP server process.
    /// The argument value factory is invoked at runtime after BeforeStartEvent has fired.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="argumentName">The name of the argument (e.g., "--url").</param>
    /// <param name="valueFactory">A factory that returns a reference expression. Called after BeforeStartEvent.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<McpBridgeResource> WithServerArgument(
        this IResourceBuilder<McpBridgeResource> builder,
        string argumentName,
        Func<ReferenceExpression> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(argumentName);
        ArgumentNullException.ThrowIfNull(valueFactory);

        // Add the argument with a prefix so it can be passed to the child process
        // Format: MCP_PROC_ARG_<name>=<value>
        // The underscore in env var name maps to hyphen in argument name
        var envVarName = argumentName.TrimStart('-').Replace("-", "_").ToUpperInvariant();
        builder.WithEnvironment(ctx => { ctx.EnvironmentVariables[$"MCP_PROC_ARG_{envVarName}"] = valueFactory(); });

        return builder;
    }

    /// <summary>
    /// Configures the namespace for the MCP server tools.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="namespace">The namespace to use for tool names.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// The namespace is used to prefix tool names to avoid conflicts when multiple MCP servers are registered.
    /// </remarks>
    public static IResourceBuilder<McpBridgeResource> WithMcpNamespace(
        this IResourceBuilder<McpBridgeResource> builder,
        string @namespace)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(@namespace);

        builder.Resource.McpNamespace = @namespace;

        return builder;
    }

    /// <summary>
    /// Configures an API key for securing the MCP bridge HTTP endpoint.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="apiKey">The API key to require in the Authorization header.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// If specified, the API key is required in the Authorization header when connecting to the bridge endpoint.
    /// </remarks>
    public static IResourceBuilder<McpBridgeResource> WithApiKey(
        this IResourceBuilder<McpBridgeResource> builder,
        string apiKey)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(apiKey);

        builder.Resource.ApiKey = apiKey;
        builder.WithEnvironment("MCP_API_KEY", apiKey);

        return builder;
    }

    /// <summary>
    /// Adds an MCP bridge for an npx-based MCP server.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="packageName">The npm package name (e.g., "@modelcontextprotocol/server-weather").</param>
    /// <param name="additionalArgs">Additional arguments to pass to the npx command.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <example>
    /// Add an npx-based weather MCP server:
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// var weatherMcp = builder.AddNpxMcpBridge("weather-mcp", "@modelcontextprotocol/server-weather");
    /// </code>
    /// </example>
    public static IResourceBuilder<McpBridgeResource> AddNpxMcpBridge(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        string packageName,
        params string[] additionalArgs)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(packageName);

        var args = new List<string> { "-y", packageName };
        if (additionalArgs != null && additionalArgs.Length > 0)
        {
            args.AddRange(additionalArgs);
        }

        return builder.AddMcpBridge(name, "npx", args.ToArray());
    }

    /// <summary>
    /// Adds an MCP bridge for a Python-based MCP server.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="moduleName">The Python module name to execute.</param>
    /// <param name="additionalArgs">Additional arguments to pass to the Python command.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <example>
    /// Add a Python-based MCP server:
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// var pyMcp = builder.AddPythonMcpBridge("my-mcp", "my_mcp_server");
    /// </code>
    /// </example>
    public static IResourceBuilder<McpBridgeResource> AddPythonMcpBridge(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        string moduleName,
        params string[] additionalArgs)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(moduleName);

        var args = new List<string> { "-m", moduleName };
        if (additionalArgs != null && additionalArgs.Length > 0)
        {
            args.AddRange(additionalArgs);
        }

        return builder.AddMcpBridge(name, "python", args.ToArray());
    }

    private static string GetCurrentRuntimeIdentifier()
    {
        var arch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture;
        
        if (OperatingSystem.IsWindows())
        {
            return arch == System.Runtime.InteropServices.Architecture.Arm64 ? "win-arm64" : "win-x64";
        }
        else if (OperatingSystem.IsLinux())
        {
            return arch == System.Runtime.InteropServices.Architecture.Arm64 ? "linux-arm64" : "linux-x64";
        }
        else if (OperatingSystem.IsMacOS())
        {
            return arch == System.Runtime.InteropServices.Architecture.Arm64 ? "osx-arm64" : "osx-x64";
        }
        
        throw new PlatformNotSupportedException($"Unsupported platform: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
    }

    private static string FindBridgeExecutable(string bridgeExeName, string rid)
    {
        var assemblyLocation = typeof(McpResourceBuilderExtensions).Assembly.Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation)!;
        
        // Option 1: NuGet package layout - tools/bridge/<rid>/executable
        var nugetPath = Path.Combine(assemblyDir, "tools", "bridge", rid, bridgeExeName);
        if (File.Exists(nugetPath))
        {
            return nugetPath;
        }
        
        // Option 2: Local development - find in Aspire.Hosting.Mcp output directory
        // Assembly might be in consuming project's output, so look for Aspire.Hosting.Mcp's output instead
        // The assembly's codebase points to the original location even when shadow-copied
        var codeBase = typeof(McpResourceBuilderExtensions).Assembly.Location;
        
        // Walk up from assembly location looking for artifacts/bin pattern
        var dir = Path.GetDirectoryName(codeBase);
        while (dir != null)
        {
            // Check if we're in an artifacts/bin structure
            if (Path.GetFileName(dir) == "artifacts")
            {
                var mcpBridgePath = Path.Combine(dir, "bin", "Aspire.Hosting.Mcp", "Debug", "net8.0", "tools", "bridge", rid, bridgeExeName);
                if (File.Exists(mcpBridgePath))
                {
                    return mcpBridgePath;
                }
                
                // Also try Release configuration
                mcpBridgePath = Path.Combine(dir, "bin", "Aspire.Hosting.Mcp", "Release", "net8.0", "tools", "bridge", rid, bridgeExeName);
                if (File.Exists(mcpBridgePath))
                {
                    return mcpBridgePath;
                }
            }
            
            dir = Path.GetDirectoryName(dir);
        }
        
        throw new FileNotFoundException(
            $"MCP bridge executable not found. Searched locations:{Environment.NewLine}" +
            $"  - {nugetPath}{Environment.NewLine}" +
            $"Ensure the Aspire.Hosting.Mcp package is properly installed, or build the Aspire.Hosting.Mcp project first for local development.");
    }
}
