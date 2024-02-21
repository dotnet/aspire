// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp.Process;
using Aspire.Hosting.Properties;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Dcp;

internal sealed partial class DcpDependencyCheck : IDcpDependencyCheckService
{
    [GeneratedRegex("[^\\d\\.].*$")]
    private static partial Regex VersionRegex();

    private readonly DistributedApplicationModel _applicationModel;
    private readonly DcpOptions _dcpOptions;

    public DcpDependencyCheck(
        DistributedApplicationModel applicationModel,
        IOptions<DcpOptions> dcpOptions)
    {
        _applicationModel = applicationModel;
        _dcpOptions = dcpOptions.Value;
    }

    public async Task EnsureDcpDependenciesAsync(CancellationToken cancellationToken = default)
    {
        var dcpPath = _dcpOptions.CliPath;
        var containerRuntime = _dcpOptions.ContainerRuntime;

        if (!File.Exists(dcpPath))
        {
            throw new FileNotFoundException($"The Aspire orchestration component is not installed at \"{dcpPath}\". The application cannot be run without it.", dcpPath);
        }

        IAsyncDisposable? processDisposable = null;
        Task<ProcessResult> task;

        try
        {
            var outputStringBuilder = new StringBuilder();

            var arguments = "info";
            if (!string.IsNullOrEmpty(containerRuntime))
            {
                arguments += $" --container-runtime {containerRuntime}";
            }

            // Run `dcp version`
            var processSpec = new ProcessSpec(dcpPath)
            {
                Arguments = arguments,
                OnOutputData = s => outputStringBuilder.Append(s),
            };

            (task, processDisposable) = ProcessUtil.Run(processSpec);

            // Disable timeout if DependencyCheckTimeout is set to zero or a negative value
            if (_dcpOptions.DependencyCheckTimeout > 0)
            {
                await task.WaitAsync(TimeSpan.FromSeconds(_dcpOptions.DependencyCheckTimeout), cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            // Parse the output as JSON
            var output = outputStringBuilder.ToString();
            if (output == string.Empty)
            {
                return; // Best effort
            }

            var dcpInfo = JsonSerializer.Deserialize<DcpInfo>(output);
            if (dcpInfo == null)
            {
                return; // Best effort
            }

            EnsureDcpVersion(dcpInfo);
            EnsureDcpContainerRuntime(dcpInfo);
        }
        catch (Exception ex) when (ex is not DistributedApplicationException)
        {
            Console.Error.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                Resources.DcpDependencyCheckFailedMessage,
                ex.ToString()
            ));
            Environment.Exit((int)DcpVersionCheckFailures.DcpVersionFailed);
        }
        finally
        {
            if (processDisposable != null)
            {
                await processDisposable.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private static void EnsureDcpVersion(DcpInfo dcpInfo)
    {
        AspireEventSource.Instance.DcpVersionCheckStart();

        try
        {
            var dcpVersionString = dcpInfo.Version;

            if (dcpVersionString == null
                || dcpVersionString == string.Empty
                || dcpVersionString == "dev")
            {
                // If empty, null, or a dev version, pass
                return;
            }

            // Early DCP versions (e.g. preview 1) have a +x at the end of their version string, e.g. 0.1.42+5,
            // which does not parse. Strip off anything like that.
            dcpVersionString = VersionRegex().Replace(dcpVersionString, string.Empty);

            if (Version.TryParse(dcpVersionString, out var dcpVersion))
            {
                if (dcpVersion < DcpVersion.MinimumVersionInclusive)
                {
                    Console.Error.WriteLine(string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.DcpVersionCheckTooLowMessage
                    ));
                    Environment.Exit((int)DcpVersionCheckFailures.DcpVersionIncompatible);
                }
            }
        }
        finally
        {
            AspireEventSource.Instance.DcpVersionCheckStop();
        }
    }

    private void EnsureDcpContainerRuntime(DcpInfo dcpInfo)
    {
        // If we don't have any resources that need a container then we
        // don't need to check for a healthy container runtime.
        if (!_applicationModel.Resources.Any(c => c.Annotations.OfType<ContainerImageAnnotation>().Any()))
        {
            return;
        }

        AspireEventSource.Instance.ContainerRuntimeHealthCheckStart();

        try
        {
            var containerRuntime = _dcpOptions.ContainerRuntime;
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
                Console.Error.WriteLine(string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.ContainerRuntimePrerequisiteMissingExceptionMessage,
                    containerRuntime,
                    error
                ));
                Environment.Exit((int)ContainerRuntimeHealthCheckFailures.PrerequisiteMissing);
            }
            else if (!running)
            {
                Console.Error.WriteLine(string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.ContainerRuntimeUnhealthyExceptionMessage,
                    containerRuntime,
                    error
                ));
                Environment.Exit((int)ContainerRuntimeHealthCheckFailures.Unhealthy);
            }

            // If we get to here all is good!
        }
        finally
        {
            AspireEventSource.Instance?.ContainerRuntimeHealthCheckStop();
        }
    }

    public class DcpInfo
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("containers")]
        public DcpContainersInfo? Containers { get; set; }
    }

    public class DcpContainersInfo
    {
        [JsonPropertyName("runtime")]
        public string? Runtime { get; set; }

        [JsonPropertyName("installed")]
        public bool Installed { get; set; } = false;

        [JsonPropertyName("running")]
        public bool Running { get; set; } = false;

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
