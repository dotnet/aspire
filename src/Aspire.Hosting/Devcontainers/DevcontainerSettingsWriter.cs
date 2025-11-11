// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using Aspire.Hosting.Devcontainers.Codespaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Devcontainers;

internal class DevcontainerSettingsWriter(ILogger<DevcontainerSettingsWriter> logger, IOptions<CodespacesOptions> codespaceOptions, IOptions<DevcontainersOptions> devcontainerOptions, IOptions<SshRemoteOptions> sshRemoteOptions) : IDisposable
{
    // Define path segments that will be combined with the user's home directory
    // These path segments are relative to the user's home directory
    // instead of hardcoding to a specific user (e.g., "vscode")
    private const string VscodeRemotePathSegment = ".vscode-remote/data/Machine/settings.json";
    private const string VscodeServerPathSegment = ".vscode-server";
    private const string VscodeInsidersServerPathSegment = ".vscode-server-insiders";
    private const string LocalDevcontainerSettingsPath = "data/Machine/settings.json";
    private const string PortAttributesFieldName = "remote.portsAttributes";
    private const int WriteLockTimeoutMs = 2000;
    private static readonly TimeSpan s_portForwardLogDelay = TimeSpan.FromSeconds(5);

    private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1);
    private readonly Channel<PortForwardEntry> _portUpdates = Channel.CreateUnbounded<PortForwardEntry>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });
    private readonly object _processingLock = new();
    private Task? _processingTask;
    private readonly CancellationTokenSource _processingCancellation = new();

    // Get the user's home directory using the Environment API
    // This ensures we work with any username in devcontainers/codespaces
    private static string GetUserHomeDirectory() =>
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public void AddPortForward(string url, int port, string protocol, string label, bool openBrowser = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);
        ArgumentException.ThrowIfNullOrEmpty(protocol);
        ArgumentException.ThrowIfNullOrEmpty(label);

        // Ensure the background processor is running.
        StartProcessingLoop();

        // Enqueue the new port forward entry.
        _portUpdates.Writer.TryWrite(new PortForwardEntry(url, port, protocol, label, openBrowser));
    }

    private void StartProcessingLoop()
    {
        lock (_processingLock)
        {
            if (_processingTask is not null)
            {
                return;
            }

            _processingTask = Task.Run(ProcessPortUpdatesAsync, _processingCancellation.Token);
        }
    }

    private async Task ProcessPortUpdatesAsync()
    {
        var reader = _portUpdates.Reader;
        try
        {
            while (await reader.WaitToReadAsync(_processingCancellation.Token).ConfigureAwait(false))
            {
                // Drain all currently available updates to batch writes and avoid excessive file I/O.
                List<PortForwardEntry> batch = [];
                while (reader.TryRead(out var entry))
                {
                    batch.Add(entry);
                }

                try
                {
                    await WriteSettingsAsync(batch, _processingCancellation.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (_processingCancellation.IsCancellationRequested)
                {
                    // Shutting down.
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error writing Devcontainer port forwarding settings batch");
                }
            }
        }
        catch (OperationCanceledException) when (_processingCancellation.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private async Task WriteSettingsAsync(IReadOnlyList<PortForwardEntry> newPorts, CancellationToken cancellationToken)
    {
        var settingsPaths = GetSettingsPaths();
        // Collect ports we actually wrote so we can log them AFTER the file save completes.
        List<(string Label, string Url)> portsToLog = [];

        foreach (var settingsPath in settingsPaths)
        {
            var acquired = await _writeLock.WaitAsync(WriteLockTimeoutMs, cancellationToken).ConfigureAwait(false);

            if (!acquired)
            {
                throw new DistributedApplicationException($"Failed to acquire semaphore for settings file: {settingsPath}");
            }

            await EnsureSettingsFileExists(settingsPath, cancellationToken).ConfigureAwait(false);

            var settingsContent = await File.ReadAllTextAsync(settingsPath, cancellationToken).ConfigureAwait(false);
            var settings = (JsonObject)JsonObject.Parse(settingsContent)!;

            JsonObject? portsAttributes;
            if (!settings.TryGetPropertyValue(PortAttributesFieldName, out var portsAttributesNode))
            {
                portsAttributes = [];
                settings.Add(PortAttributesFieldName, portsAttributes);
            }
            else
            {
                portsAttributes = (JsonObject)portsAttributesNode!;
            }

            // Data is keyed by port number, but we want to key it by label
            // e.g
            // {
            //     "remote.portsAttributes": {
            //         "8080": {
            //             "label": "MyApp",
            //             "protocol": "http",
            //             "onAutoForward": "openBrowser"
            //         }
            //     }
            // }

            var portsByLabel = (from props in portsAttributes
                                let attrs = props.Value as JsonObject
                                let forwardedPort = props.Key
                                let l = attrs["label"]?.ToString()
                                where l != null
                                select new { Label = l, Port = forwardedPort })
                                .ToLookup(p => p.Label, p => p.Port);

            foreach (var portEntry in newPorts)
            {
                var port = portEntry.Port.ToString(CultureInfo.InvariantCulture);
                var label = portEntry.Label;
                var protocol = portEntry.Protocol;
                var openBrowser = portEntry.OpenBrowser;
                var url = portEntry.Url;
                // Remove any existing ports with the same label
                foreach (var oldPort in portsByLabel[label])
                {
                    portsAttributes.Remove(oldPort);
                }

                JsonObject? portAttributes;
                if (!portsAttributes.TryGetPropertyValue(port, out var portAttributeNode))
                {
                    portAttributes = [];
                    portsAttributes.Add(port, portAttributes);
                }
                else
                {
                    portAttributes = (JsonObject)portAttributeNode!;
                }

                portAttributes["label"] = label;
                portAttributes["protocol"] = protocol;
                portAttributes["onAutoForward"] = openBrowser ? "openBrowser" : "silent";

                portsToLog.Add((label, url));
            }

            settingsContent = settings.ToString();
            await File.WriteAllTextAsync(settingsPath, settingsContent, cancellationToken).ConfigureAwait(false);

            _writeLock.Release();
        }

        if (portsToLog.Count > 0)
        {
            // Delay logging until after the settings file(s) have been updated for at least s_portForwardLogDelay.
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(s_portForwardLogDelay, cancellationToken).ConfigureAwait(false);
                    foreach (var (label, url) in portsToLog)
                    {
                        logger.LogInformation("Port forwarding ({label}): {Url}", label, url);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Ignore cancellation
                }
            }, cancellationToken);
        }

        IEnumerable<string> GetSettingsPaths()
        {
            // Get the current user's home directory
            // This ensures we work with any username in the container, not just "vscode"
            var userHomeDir = GetUserHomeDirectory();

            // For some reason the machine settings path is different between Codespaces and local Devcontainers
            // so we decide which one to use here based on the options.
            if (codespaceOptions.Value.IsCodespace)
            {
                yield return Path.Combine(userHomeDir, VscodeRemotePathSegment);
            }
            else if (devcontainerOptions.Value.IsDevcontainer || sshRemoteOptions.Value.IsSshRemote)
            {
                var vscodeServerPath = Path.Combine(userHomeDir, VscodeServerPathSegment);
                var vscodeInsidersServerPath = Path.Combine(userHomeDir, VscodeInsidersServerPathSegment);

                if (Directory.Exists(vscodeServerPath))
                {
                    yield return Path.Combine(vscodeServerPath, LocalDevcontainerSettingsPath);
                }

                if (Directory.Exists(vscodeInsidersServerPath))
                {
                    yield return Path.Combine(vscodeInsidersServerPath, LocalDevcontainerSettingsPath);
                }
            }
            else
            {
                throw new DistributedApplicationException("Codespaces, Devcontainer, or SSH Remote not detected.");
            }
        }

        async Task EnsureSettingsFileExists(string path, CancellationToken cancellationToken)
        {
            try
            {
                if (!File.Exists(path))
                {
                    // Ensure the parent directory exists before attempting to create the file
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                    // The extra ceremony here is to avoid accidentally overwriting the file if it was
                    // created after we checked for its existence. If the file exists when we go to write
                    // it then we will throw and log a warning, but otherwise continue executing.
                    using var stream = File.Open(path, FileMode.CreateNew);
                    using var writer = new StreamWriter(stream);
                    await writer.WriteAsync("{}".AsMemory(), cancellationToken).ConfigureAwait(false);
                }
            }
            catch (IOException ex) when (ex.Message == $"The file '{path}' already exists.")
            {
                // This is OK, but it should be rare enough that if it starts happening we probably
                // want to know about it in logs that end users submit so we know to take a closer
                // look at what is going on.
                logger.LogWarning("Race condition detected when creating Devcontainer settings file: {Path}", path);
            }
        }
    }

    public void Dispose()
    {
        _processingCancellation.Cancel();
        _portUpdates.Writer.TryComplete();
    }

    private sealed record PortForwardEntry(string Url, int Port, string Protocol, string Label, bool OpenBrowser);
}
