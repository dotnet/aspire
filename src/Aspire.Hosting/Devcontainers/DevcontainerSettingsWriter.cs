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
    private const string CodespaceSettingsPath = "/home/vscode/.vscode-remote/data/Machine/settings.json";
    private const string VSCodeServerPath = "/home/vscode/.vscode-server";
    private const string VSCodeInsidersServerPath = "/home/vscode/.vscode-server-insiders";
    private const string LocalDevcontainerSettingsPath = "data/Machine/settings.json";
    private const string PortAttributesFieldName = "remote.portsAttributes";
    private const int WriteLockTimeoutMs = 2000;
    private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1);

    public async Task SetPortAttributesAsync(int port, string protocol, string label, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(protocol);
        ArgumentNullException.ThrowIfNullOrEmpty(label);

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
                portsAttributes = new JsonObject();
                settings.Add(PortAttributesFieldName, portsAttributes);
            }
            else
            {
                portsAttributes = (JsonObject)portsAttributesNode!;
            }

            var portAsString = port.ToString(CultureInfo.InvariantCulture);

            JsonObject? portAttributes;
            if (!portsAttributes.TryGetPropertyValue(portAsString, out var portAttributeNode))
            {
                portAttributes = new JsonObject();
                portsAttributes.Add(portAsString, portAttributes);
            }
            else
            {
                portAttributes = (JsonObject)portAttributeNode!;
            }

            portAttributes["label"] = label;
            portAttributes["protocol"] = protocol;
            portAttributes["onAutoForward"] = "notify";

            settingsContent = settings.ToString();
            await File.WriteAllTextAsync(settingsPath, settingsContent, cancellationToken).ConfigureAwait(false);

            _writeLock.Release();
        }

        IEnumerable<string> GetSettingsPaths()
        {
            // For some reason the machine settings path is different between Codespaces and local Devcontainers
            // so we decide which one to use here based on the options.
            if (codespaceOptions.Value.IsCodespace)
            {
                yield return CodespaceSettingsPath;
            }
            else if (devcontainerOptions.Value.IsDevcontainer)
            {
                if (Directory.Exists(VSCodeServerPath))
                {
                    yield return Path.Combine(VSCodeServerPath, LocalDevcontainerSettingsPath);
                }

                if (Directory.Exists(VSCodeInsidersServerPath))
                {
                    yield return Path.Combine(VSCodeInsidersServerPath, LocalDevcontainerSettingsPath);
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
