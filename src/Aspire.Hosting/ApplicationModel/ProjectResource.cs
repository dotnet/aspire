#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dcp.Process;
using Aspire.Hosting.Exec;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a specified .NET project.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class ProjectResource(string name)
    : Resource(name), IResourceWithEnvironment, IResourceWithArgs, IResourceWithServiceDiscovery, IResourceWithWaitSupport,
    IComputeResource, IResourceSupportsExec
{
    // Keep track of the config host for each Kestrel endpoint annotation
    internal Dictionary<EndpointAnnotation, string> KestrelEndpointAnnotationHosts { get; } = new();

    // Are there any endpoints coming from Kestrel configuration
    internal bool HasKestrelEndpoints => KestrelEndpointAnnotationHosts.Count > 0;

    // Track the https endpoint that was added as a default, and should be excluded from the port & kestrel environment
    internal EndpointAnnotation? DefaultHttpsEndpoint { get; set; }

    internal bool ShouldInjectEndpointEnvironment(EndpointReference e)
    {
        var endpoint = e.EndpointAnnotation;

        if (endpoint.UriScheme is not ("http" or "https") ||    // Only process http and https endpoints
            endpoint.TargetPortEnvironmentVariable is not null) // Skip if target port env variable was set
        {
            return false;
        }

        // If any filter rejects the endpoint, skip it
        return !Annotations.OfType<EndpointEnvironmentInjectionFilterAnnotation>()
            .Select(a => a.Filter)
            .Any(f => !f(endpoint));
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(ExecOptions options, ILogger logger, IDisposable? loggerDisposable, CancellationToken cancellationToken)
    {
        var projectMetadata = this.GetProjectMetadata();

        var (exe, args) = ParseCommand();
        // var env = await BuildEnvironmentAsync().ConfigureAwait(false);
        var env = new Dictionary<string, string>();

        var processSpec = new ProcessSpec(exe)
        {
            Arguments = args,
            EnvironmentVariables = env,
            WorkingDirectory = Path.GetDirectoryName(projectMetadata.ProjectPath),
            OnOutputData = data => logger.Log(LogLevel.Information, data),
            OnErrorData = data => logger.Log(LogLevel.Error, data)
        };

        int exitCode = -1;
        try
        {
            var (processResultTask, disposable) = ProcessUtil.Run(processSpec);
            var result = await processResultTask.ConfigureAwait(false);
            exitCode = result.ExitCode;
            
            await disposable.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Command {command} failed", options.Command);
        }
        finally
        {
            logger.LogInformation("exec '{command}' finished with exitCode {exitCode}", options.Command, exitCode);
            loggerDisposable?.Dispose();
        }
        

        (string exe, string args) ParseCommand()
        {
            var split = options.Command.Split(' ', count: 2);
            return (split[0].Trim('"'), split[1].Trim('"'));
        }
    }
}
