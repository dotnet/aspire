// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json.Nodes;

namespace Aspire.Hosting.Devcontainers;

internal class DevcontainerPortForwardingHelper
{
    private const string MachineSettingsPath = "/home/vscode/.vscode-remote/data/Machine/settings.json";
    private const string PortAttributesFieldName = "remote.portsAttributes";

    public static async Task SetPortAttributesAsync(int port, string protocol, string label, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(protocol);
        ArgumentNullException.ThrowIfNullOrEmpty(label);

        var settingsContent = await File.ReadAllTextAsync(MachineSettingsPath, cancellationToken).ConfigureAwait(false);
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
        await File.WriteAllTextAsync(MachineSettingsPath, settingsContent, cancellationToken).ConfigureAwait(false);
    }
}