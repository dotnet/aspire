// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json.Nodes;

namespace Aspire.Hosting.Devcontainers;

internal class DevcontainerPortForwardingHelper
{
    private const string CodespaceSettingsPath = "/home/vscode/.vscode-remote/data/Machine/settings.json";
    private const string LocalDevcontainerSettingsPath = "/home/vscode/.vscode-server/data/Machine/settings.json";
    private const string PortAttributesFieldName = "remote.portsAttributes";

    public static async Task SetPortAttributesAsync(int port, string protocol, string label, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(protocol);
        ArgumentNullException.ThrowIfNullOrEmpty(label);

        var settingsPath = GetSettingsPath();

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

        static string GetSettingsPath()
        {
            // For some reason the machine settings path is different between Codespaces and local Devcontainers
            // so we probe for it here. We could use options to figure out which one to look for here but that
            // would require taking a dependency on the options system here which seems like overkill.
            if (File.Exists(CodespaceSettingsPath))
            {
                return CodespaceSettingsPath;
            }
            else if (File.Exists(LocalDevcontainerSettingsPath))
            {
                return LocalDevcontainerSettingsPath;
            }
            else
            {
                throw new DistributedApplicationException("Could not find a devcontainer settings file.");
            }
        }
    }
}