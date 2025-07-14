// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json.Nodes;
using Aspire.Hosting.Devcontainers.Codespaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Devcontainers;

internal class DevcontainerSettingsWriter(ILogger<DevcontainerSettingsWriter> logger, IOptions<CodespacesOptions> codespaceOptions, IOptions<DevcontainersOptions> devcontainerOptions)
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
    private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1);
    
    // Get the user's home directory using the Environment API
    // This ensures we work with any username in devcontainers/codespaces
    private static string GetUserHomeDirectory() => 
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    private readonly List<(string Url, string Port, string Protocol, string Label, bool OpenBrowser)> _pendingPorts = [];

    public void AddPortForward(string url, int port, string protocol, string label, bool openBrowser = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);
        ArgumentException.ThrowIfNullOrEmpty(protocol);
        ArgumentException.ThrowIfNullOrEmpty(label);

        _pendingPorts.Add((url, port.ToString(CultureInfo.InvariantCulture), protocol, label, openBrowser));
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        await WriteSettingsAsync(cancellationToken).ConfigureAwait(false);

        // Don't block the caller on this task, we just want to log the port forwards after a delay.
        _ = Task.Run(async () =>
        {
            // HACK: VS code needs to read an updated settings file before it will pick up the port forwards
            // we're logging here. This is a hack to give it time to do that.
            await Task.Delay(5000, cancellationToken).ConfigureAwait(false);

            // This is how VS code finds out about the port forwards in hybrid mode (output + proccess).
            foreach (var (url, _, _, label, _) in _pendingPorts)
            {
                logger.LogInformation("Port forwarding ({label}): {Url}", label, url);
            }

        }, cancellationToken);
    }

    private async Task WriteSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settingsPaths = GetSettingsPaths();

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

            foreach (var (url, port, protocol, label, openBrowser) in _pendingPorts)
            {
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
            }

            settingsContent = settings.ToString();
            await File.WriteAllTextAsync(settingsPath, settingsContent, cancellationToken).ConfigureAwait(false);

            _writeLock.Release();
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
            else if (devcontainerOptions.Value.IsDevcontainer)
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
                throw new DistributedApplicationException("Codespaces or Devcontainer not detected.");
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
}
