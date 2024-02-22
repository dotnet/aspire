// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding JavaScript runtime based applications to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class JavaScriptAppHostingExtension
{   
    /// <summary>
    /// Adds a JavaScript runtime to the application model. The runtime and dependencies should available on the PATH.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resource to.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="scriptPath">The path to the script that the runtime will execute.</param>
    /// <param name="workingDirectory">The working directory to use for the command. If null, the working directory of the current process is used.</param>
    /// <param name="args">The arguments to pass to the command.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<JavaScriptAppResource> AddJavaScriptApp(this IDistributedApplicationBuilder builder, string name, string? scriptPath = null, string? workingDirectory = null, string[]? args = null)
    {   
        scriptPath ??= "node";
        args ??= [];
        string[] effectiveArgs = [scriptPath, .. args];
        workingDirectory ??= Path.GetDirectoryName(scriptPath)!;
        workingDirectory = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, workingDirectory));

        var resource = new JavaScriptAppResource(name, scriptPath, workingDirectory, effectiveArgs);

        return builder.AddResource(resource)
                      .WithJavaScriptAppDefaults();
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
    public static IResourceBuilder<JavaScriptAppResource> AddJavaScriptCLIApp(this IDistributedApplicationBuilder builder, string name, string workingDirectory, string? scriptName = null, string[]? args = null)
    {   
        args ??= [];
        scriptName ??= "npm";
        string[] allArgs;

        // Check if scriptName is "npm" and adjust allArgs accordingly
        if (string.Equals(scriptName, "npm", StringComparison.OrdinalIgnoreCase))
        {
            allArgs = args is { Length: > 0 }
                ? new string[] { "run", scriptName, "--" }.Concat(args).ToArray()
                : new string[] { "run", scriptName };
        }
        else
        {
            // If scriptName is not npm, use developer config or an empty array
            allArgs = args ?? Array.Empty<string>();
        }

        workingDirectory = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, workingDirectory));

        var resource = new JavaScriptAppResource(name, scriptName, workingDirectory, allArgs);

        return builder.AddResource(resource)
                      .WithJavaScriptAppDefaults();
    }

    // this needs tested only for node since
    // other runtimes may use a different pattern for env configuration
     private static IResourceBuilder<JavaScriptAppResource> WithJavaScriptAppDefaults(this IResourceBuilder<JavaScriptAppResource> builder)
    {
        var environment = builder.ApplicationBuilder.Environment;

        if (builder.Resource.command.Equals("node", StringComparison.OrdinalIgnoreCase))
        {
            return builder.WithOtlpExporter()
                            .WithEnvironment("NODE_ENV", environment.IsDevelopment() ? "development" : "production");
        }

        return builder;
    }
}
