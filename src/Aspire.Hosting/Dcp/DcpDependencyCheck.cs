// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Aspire.Hosting.Dcp.Process;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using Aspire.Hosting.Properties;

namespace Aspire.Hosting.Dcp;

internal sealed partial class DcpDependencyCheck
{
    // Docker goes to into resource saver mode after 5 minutes of not running a container (by default).
    // While in this mode, the commands we use for the docker runtime checks can take quite some time
    private const int WaitTimeForDcpInfoCommandInSeconds = 10;

    [GeneratedRegex("[^\\d\\.].*$")]
    private static partial Regex VersionRegex();

    public static async Task EnsureDcpDependenciesAsync(string? dcpPath, bool hasContainers, string? containerRuntime, CancellationToken cancellationToken)
    {
        if (!File.Exists(dcpPath))
        {
            throw new FileNotFoundException($"The Aspire application host is not installed at \"{dcpPath}\". The application cannot be run without it.", dcpPath);
        }

        try
        {
            await EnsureDcpVersionAsync(dcpPath, cancellationToken).ConfigureAwait(false);
            await EnsureDcpContainerRuntimeAsync(dcpPath, hasContainers, containerRuntime, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not DistributedApplicationException)
        {
            Console.Error.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                Resources.DcpDependencyCheckFailedMessage,
                ex.ToString()
            ));
        }
    }

    private static async Task EnsureDcpVersionAsync(string dcpPath, CancellationToken cancellationToken)
    {
        AspireEventSource.Instance.DcpVersionCheckStart();

        IAsyncDisposable? processDisposable = null;
        Task<ProcessResult> task;

        try
        {
            var outputStringBuilder = new StringBuilder();

            var arguments = "version";

            // Run `dcp version`
            var processSpec = new ProcessSpec(dcpPath)
            {
                Arguments = arguments,
                OnOutputData = s => outputStringBuilder.Append(s),
            };

            (task, processDisposable) = ProcessUtil.Run(processSpec);

            await task.WaitAsync(TimeSpan.FromSeconds(WaitTimeForDcpInfoCommandInSeconds), cancellationToken).ConfigureAwait(false);

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

            string? dcpVersionString = dcpInfo.Version;

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

            Version? dcpVersion;
            if (Version.TryParse(dcpVersionString, out dcpVersion))
            {
                if (dcpVersion < DcpVersion.MinimumVersionInclusive)
                {
                    Console.Error.WriteLine(string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.DcpVersionCheckTooLowMessage
                    ));
                    Environment.Exit((int)DcpVersionCheckFailures.DcpVersionIncompatible);
                }
                else if (dcpVersion >= DcpVersion.MaximumVersionExclusive)
                {
                    Console.Error.WriteLine(string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.DcpVersionCheckTooHighMessage,
                        DcpVersion.MinimumVersionInclusive.ToString()
                    ));
                    Environment.Exit((int)DcpVersionCheckFailures.DcpVersionIncompatible);
                }
            }
        }
        finally
        {
            if (processDisposable != null)
            {
                await processDisposable.DisposeAsync().ConfigureAwait(false);
            }

            AspireEventSource.Instance.DcpVersionCheckStop();
        }
    }

    private static async Task EnsureDcpContainerRuntimeAsync(string dcpPath, bool hasContainers, string? containerRuntime, CancellationToken cancellationToken)
    {
        // If we don't have any resources that need a container then we
        // don't need to check for a healthy container runtime.
        if (!hasContainers)
        {
            return;
        }

        AspireEventSource.Instance.ContainerRuntimeHealthCheckStart();

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

            await task.WaitAsync(TimeSpan.FromSeconds(WaitTimeForDcpInfoCommandInSeconds), cancellationToken).ConfigureAwait(false);

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

            containerRuntime ??= "docker";
            bool installed = dcpInfo.Containers?.Installed ?? false;
            bool running = dcpInfo.Containers?.Running ?? false;
            string? error = dcpInfo.Containers?.Error;

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
            if (processDisposable != null)
            {
                await processDisposable.DisposeAsync().ConfigureAwait(false);
            }

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
