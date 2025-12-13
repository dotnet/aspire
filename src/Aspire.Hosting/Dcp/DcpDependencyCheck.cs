// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREHOSTINGVIRTUALSHELL001

using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Aspire.Hosting.Resources;
using Aspire.Hosting.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Dcp;

internal sealed partial class DcpDependencyCheck : IDcpDependencyCheckService
{
    [GeneratedRegex("[^\\d\\.].*$")]
    private static partial Regex VersionRegex();

    private readonly DcpOptions _dcpOptions;
    private readonly IVirtualShell _shell;
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
    private DcpInfo? _dcpInfo;
    private bool _checkDone;

    public DcpDependencyCheck(IOptions<DcpOptions> dcpOptions, IVirtualShell shell)
    {
        _dcpOptions = dcpOptions.Value;
        _shell = shell;
    }

    public async Task<DcpInfo?> GetDcpInfoAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        AspireEventSource.Instance.DcpInfoFetchStart(force);
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_checkDone && !force)
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

            try
            {
                var args = new List<string> { "info" };
                if (!string.IsNullOrEmpty(containerRuntime))
                {
                    args.Add("--container-runtime");
                    args.Add(containerRuntime);
                }

                // Create timeout CTS if needed, linked to the caller's cancellation token
                using var timeoutCts = _dcpOptions.DependencyCheckTimeout > 0
                    ? new CancellationTokenSource(TimeSpan.FromSeconds(_dcpOptions.DependencyCheckTimeout))
                    : null;

                using var linkedCts = timeoutCts != null
                    ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token)
                    : null;

                var effectiveCt = linkedCts?.Token ?? cancellationToken;
                var result = await _shell.RunAsync(dcpPath, args, effectiveCt).ConfigureAwait(false);

                if (result.ExitCode != 0)
                {
                    throw new DistributedApplicationException(string.Format(
                        CultureInfo.InvariantCulture,
                        MessageStrings.DcpDependencyCheckFailedMessage,
                        $"'dcp info' returned exit code {result.ExitCode}. {result.Stderr}{Environment.NewLine}{result.Stdout}"
                    ));
                }

                // Parse the output as JSON
                var output = result.Stdout ?? string.Empty;
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
            catch (Exception ex) when
                (ex is not DistributedApplicationException
                && !(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
            {
                throw new DistributedApplicationException(string.Format(
                    CultureInfo.InvariantCulture,
                    MessageStrings.DcpDependencyCheckFailedMessage,
                    ex.Message
                ));
            }
        }
        finally
        {
            _lock.Release();
            AspireEventSource.Instance.DcpInfoFetchStop(force);
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
                        MessageStrings.DcpVersionCheckTooLowMessage,
                        GetCurrentPackageVersion(typeof(DcpDependencyCheck).Assembly)
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

    private static string GetCurrentPackageVersion(Assembly assembly)
    {
        // The package version is stamped into the assembly's AssemblyInformationalVersionAttribute at build time, followed by a '+' and
        // the commit hash, e.g.:
        // [assembly: AssemblyInformationalVersion("9.1.0-preview.1.25111.1+ad18db0213e9db8209bca0feb83fc801f34634f5)]

        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (version is not null)
        {
            var plusIndex = version.IndexOf('+');

            if (plusIndex > 0)
            {
                return version[..plusIndex];
            }

            return version;
        }

        // Fallback to the first 3 parts of the assembly version
        if (Version.TryParse(assembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version, out var assemblyVersion))
        {
            return assemblyVersion.ToString(3);
        }

        return "<unknown>";
    }

    internal static (string message, string? linkUrl) BuildContainerRuntimeUnhealthyMessage(string containerRuntime)
    {
        var messageBuilder = new StringBuilder();
        messageBuilder.AppendFormat(CultureInfo.InvariantCulture, InteractionStrings.ContainerRuntimeUnhealthyMessage, containerRuntime);
        string? linkUrl = null;

        if (string.Equals(containerRuntime, "docker", StringComparison.OrdinalIgnoreCase))
        {
            messageBuilder.Append(InteractionStrings.ContainerRuntimeDockerAdvice);
            linkUrl = "https://docs.docker.com/desktop/use-desktop/resource-saver/";
        }
        else if (string.Equals(containerRuntime, "podman", StringComparison.OrdinalIgnoreCase))
        {
            messageBuilder.Append(InteractionStrings.ContainerRuntimePodmanAdvice);
        }
        else
        {
            messageBuilder.Append(InteractionStrings.ContainerRuntimeGenericAdvice);
        }

        return (messageBuilder.ToString(), linkUrl);
    }

    internal static void CheckDcpInfoAndLogErrors(ILogger logger, DcpOptions options, DcpInfo dcpInfo, bool throwIfUnhealthy = false)
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
            logger.LogWarning("Container runtime '{Runtime}' could not be found. See https://aka.ms/dotnet/aspire/containers for more details on supported container runtimes.", containerRuntime);

            logger.LogDebug("The error from the container runtime check was: {Error}", error);
            if (throwIfUnhealthy)
            {
                throw new DistributedApplicationException($"Container runtime '{containerRuntime}' could not be found. See https://aka.ms/dotnet/aspire/containers for more details on supported container runtimes.");
            }
        }
        else if (!running)
        {
            var (message, linkUrl) = BuildContainerRuntimeUnhealthyMessage(containerRuntime);

            // For logging, we want the template format with {Runtime} placeholder
            var logMessage = message.Replace($"'{containerRuntime}'", "'{Runtime}'");
            if (linkUrl is not null)
            {
                logMessage += " For more information, visit: " + linkUrl;
            }

            logger.LogWarning(logMessage, containerRuntime);

            logger.LogDebug("The error from the container runtime check was: {Error}", error);
            if (throwIfUnhealthy)
            {
                throw new DistributedApplicationException(message);
            }
        }
    }
}
