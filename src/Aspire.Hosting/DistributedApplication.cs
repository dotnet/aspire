// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Properties;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

public enum DockerHealthCheckFailures : int
{
    Unresponsive = 125, // Invocation of Docker CLI test command did not finish within expected time period.
    Unhealthy = 126,    // The Docker CLI test command returned an error exit code.
    PrerequisiteMissing = 127 // We could not invoke Docker CLI, Docker may be missing from the machine.
}

[DebuggerDisplay("{_host}")]
public class DistributedApplication : IHost, IAsyncDisposable
{
    private readonly IHost _host;
    private readonly string[] _args;

    public DistributedApplication(IHost host, string[] args)
    {
        _host = host;
        _args = args;
    }

    public static IDistributedApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = new DistributedApplicationBuilder(args);
        return builder;
    }

    public IServiceProvider Services => _host.Services;

    public void Dispose()
    {
        _host.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return ((IAsyncDisposable)_host).DisposeAsync();
    }

    private const int WaitTimeForDockerTestCommandInSeconds = 10;

    private void EnsureDockerIfNecessary()
    {
        // If we don't have any respirces that need a container  then we
        // don't need to check for Docker.
        var appModel = this.Services.GetRequiredService<DistributedApplicationModel>();
        if (!appModel.Resources.Any(c => c.Annotations.OfType<ContainerImageAnnotation>().Any()))
        {
            return;
        }

        AspireEventSource.Instance.DockerHealthCheckStart();

        try
        {
            var dockerCommandArgs = "ps --latest --quiet";
            var dockerStartInfo = new ProcessStartInfo()
            {
                FileName = FileUtil.FindFullPathFromPath("docker"),
                Arguments = dockerCommandArgs,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            var process = System.Diagnostics.Process.Start(dockerStartInfo);
            if (process is { } && process.WaitForExit(TimeSpan.FromSeconds(WaitTimeForDockerTestCommandInSeconds)))
            {
                if (process.ExitCode != 0)
                {
                    Console.Error.WriteLine(string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.DockerUnhealthyExceptionMessage,
                        $"docker {dockerCommandArgs}",
                        process.ExitCode
                    ));
                    Environment.Exit((int)DockerHealthCheckFailures.Unhealthy);
                }
            }
            else
            {
                Console.Error.WriteLine(string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.DockerUnresponsiveExceptionMessage,
                    $"docker {dockerCommandArgs}",
                    WaitTimeForDockerTestCommandInSeconds
                ));
                Environment.Exit((int)DockerHealthCheckFailures.Unresponsive);
            }

            // If we get to here all is good!

        }
        catch (Exception ex) when (ex is not DistributedApplicationException)
        {
            Console.Error.WriteLine(string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.DockerPrerequisiteMissingExceptionMessage,
                    ex.ToString()
                ));
            Environment.Exit((int)DockerHealthCheckFailures.PrerequisiteMissing);
        }
        finally
        {
            AspireEventSource.Instance?.DockerHealthCheckStop();
        }
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureDockerIfNecessary();
            await ExecuteBeforeStartHooksAsync(cancellationToken).ConfigureAwait(false);
            await _host.RunAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (_host is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                _host.Dispose();
            }
        }
    }

    public void Run()
    {
        RunAsync().Wait();
    }

    private async Task ExecuteBeforeStartHooksAsync(CancellationToken cancellationToken)
    {
        AspireEventSource.Instance.AppBeforeStartHooksStart();

        try
        {
            var lifecycleHooks = _host.Services.GetServices<IDistributedApplicationLifecycleHook>();
            var appModel = _host.Services.GetRequiredService<DistributedApplicationModel>();

            foreach (var lifecycleHook in lifecycleHooks)
            {
                await lifecycleHook.BeforeStartAsync(appModel, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            AspireEventSource.Instance.AppBeforeStartHooksStop();
        }
    }

    Task IHost.StartAsync(CancellationToken cancellationToken) => _host.StartAsync(cancellationToken);

    Task IHost.StopAsync(CancellationToken cancellationToken) => _host.StopAsync(cancellationToken);
}

