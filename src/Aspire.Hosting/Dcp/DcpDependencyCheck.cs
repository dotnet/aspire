// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Aspire.Hosting.Dcp.Process;
using Aspire.Hosting.Properties;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Dcp;

internal sealed partial class DcpDependencyCheck : IDcpDependencyCheckService
{
    [GeneratedRegex("[^\\d\\.].*$")]
    private static partial Regex VersionRegex();

    private readonly DcpOptions _dcpOptions;
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
    private DcpInfo? _dcpInfo;
    private bool _checkDone;

    public DcpDependencyCheck(IOptions<DcpOptions> dcpOptions)
    {
        _dcpOptions = dcpOptions.Value;
    }

    public async Task<DcpInfo?> GetDcpInfoAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_checkDone)
            {
                return _dcpInfo;
            }
            _checkDone = true;

            var dcpPath = _dcpOptions.CliPath;
            var containerRuntime = _dcpOptions.ContainerRuntime;

            if (!File.Exists(dcpPath))
            {
                throw new FileNotFoundException($"The Aspire orchestration component is not installed at \"{dcpPath}\". The application cannot be run without it.", dcpPath);
            }

            IAsyncDisposable? processDisposable = null;
            Task<ProcessResult> task;
            var outputStringBuilder = new StringBuilder();
            var errorStringBuilder = new StringBuilder();

            try
            {
                var arguments = "info";
                if (!string.IsNullOrEmpty(containerRuntime))
                {
                    arguments += $" --container-runtime {containerRuntime}";
                }

                var processSpec = new ProcessSpec(dcpPath)
                {
                    Arguments = arguments,
                    OnOutputData = s => outputStringBuilder.Append(s),
                    OnErrorData = s => errorStringBuilder.Append(s),
                    ThrowOnNonZeroReturnCode = false
                };

                (task, processDisposable) = ProcessUtil.Run(processSpec);
                ProcessResult processResult;

                // Disable timeout if DependencyCheckTimeout is set to zero or a negative value
                if (_dcpOptions.DependencyCheckTimeout > 0)
                {
                    processResult = await task.WaitAsync(TimeSpan.FromSeconds(_dcpOptions.DependencyCheckTimeout), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    processResult = await task.WaitAsync(cancellationToken).ConfigureAwait(false);
                }

                if (processResult.ExitCode != 0)
                {
                    throw new DistributedApplicationException(string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.DcpDependencyCheckFailedMessage,
                        $"'dcp {arguments}' returned exit code {processResult.ExitCode}. {errorStringBuilder.ToString()}{Environment.NewLine}{outputStringBuilder.ToString()}"
                    ));
                }

                // Parse the output as JSON
                var output = outputStringBuilder.ToString();
                if (output == string.Empty)
                {
                    return null; // Best effort
                }

                var dcpInfo = JsonSerializer.Deserialize<DcpInfo>(output);
                if (dcpInfo == null)
                {
                    return null; // Best effort
                }

                EnsureDcpVersion(dcpInfo);
                _dcpInfo = dcpInfo;
                return dcpInfo;
            }
            catch (Exception ex) when (ex is not DistributedApplicationException)
            {
                throw new DistributedApplicationException(string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.DcpDependencyCheckFailedMessage,
                    $"{ex.Message} {errorStringBuilder.ToString()}{Environment.NewLine}{outputStringBuilder.ToString()}"
                ));
            }
            finally
            {
                if (processDisposable != null)
                {
                    try
                    {
                        await processDisposable.DisposeAsync().ConfigureAwait(false);
                    }
                    catch { } // Dispose (dcp info process termination) is best effort.
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private static void EnsureDcpVersion(DcpInfo dcpInfo)
    {
        AspireEventSource.Instance.DcpVersionCheckStart();

        try
        {
            var dcpVersionString = dcpInfo.VersionString;

            if (dcpVersionString == null
                || dcpVersionString == string.Empty
                || dcpVersionString == "dev")
            {
                // If empty, null, or a dev version, pass
                dcpInfo.Version = DcpVersion.Dev;
                return;
            }

            // Early DCP versions (e.g. preview 1) have a +x at the end of their version string, e.g. 0.1.42+5,
            // which does not parse. Strip off anything like that.
            dcpVersionString = VersionRegex().Replace(dcpVersionString, string.Empty);

            if (Version.TryParse(dcpVersionString, out var dcpVersion))
            {
                if (dcpVersion < DcpVersion.MinimumVersionInclusive)
                {
                    throw new DistributedApplicationException(string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.DcpVersionCheckTooLowMessage
                    ));
                }

                dcpInfo.Version = dcpVersion;
            }
        }
        finally
        {
            AspireEventSource.Instance.DcpVersionCheckStop();
        }
    }

    internal static void CheckDcpInfoAndLogErrors(ILogger logger, DcpOptions options, DcpInfo dcpInfo)
    {
        var containerRuntime = options.ContainerRuntime;
        if (string.IsNullOrEmpty(containerRuntime))
        {
            // Default runtime is Docker
            containerRuntime = "docker";
        }
        var installed = dcpInfo.Containers?.Installed ?? false;
        var running = dcpInfo.Containers?.Running ?? false;
        var error = dcpInfo.Containers?.Error;

        if (!installed)
        {
            logger.LogCritical("Container runtime '{runtime}' could not be found. See https://aka.ms/dotnet/aspire/containers for more details on supported container runtimes.", containerRuntime);

            logger.LogDebug("The error from the container runtime check was: {error}", error);
        }
        else if (!running)
        {
            var messageFormat = new StringBuilder();
            messageFormat.Append("Container runtime '{runtime}' was found but appears to be unhealthy. ");

            if (string.Equals(containerRuntime, "docker", StringComparison.OrdinalIgnoreCase))
            {
                messageFormat.Append("Ensure that Docker is running and that the Docker daemon is accessible. ");
                messageFormat.Append("If Resource Saver mode is enabled, containers may not run. For more information, visit: https://docs.docker.com/desktop/use-desktop/resource-saver/");
            }
            else if (string.Equals(containerRuntime, "podman", StringComparison.OrdinalIgnoreCase))
            {
                messageFormat.Append("Ensure that Podman is running.");
            }
            else
            {
                messageFormat.Append("Ensure that the container runtime is running.");
            }

            logger.LogCritical(messageFormat.ToString(), containerRuntime);

            logger.LogDebug("The error from the container runtime check was: {error}", error);
        }
    }
}
