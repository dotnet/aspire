// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Devcontainers.Codespaces;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Devcontainers;

internal sealed class DevcontainerPortForwardingLifecycleHook : IDistributedApplicationLifecycleHook
{
    private const string MachineSettingsPath = "/home/vscode/.vscode-remote/data/Machine/settings.json";
    private const string PortAttributesFieldName = "remote.portsAttributes";

    private readonly ILogger _hostingLogger;
    private readonly IOptions<CodespacesOptions> _codespacesOptions;
    private readonly IOptions<DevcontainersOptions> _devcontainersOptions;

    public DevcontainerPortForwardingLifecycleHook(ILoggerFactory loggerFactory, IOptions<CodespacesOptions> codespacesOptions, IOptions<DevcontainersOptions> devcontainersOptions)
    {
        _hostingLogger = loggerFactory.CreateLogger("Aspire.Hosting");
        _codespacesOptions = codespacesOptions;
        _devcontainersOptions = devcontainersOptions;
    }

    public async Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        if (!_devcontainersOptions.Value.IsDevcontainer && !_codespacesOptions.Value.IsCodespace)
        {
            // We aren't a codespace so there is nothing to do here.
            return;
        }

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

        var urlsToAnnounce = new List<string>();
        foreach (var resource in appModel.Resources)
        {
            if (resource is not IResourceWithEndpoints resourceWithEndpoints)
            {
                continue;
            }

            foreach (var endpoint in resourceWithEndpoints.Annotations.OfType<EndpointAnnotation>())
            {
                if (_codespacesOptions.Value.IsCodespace && endpoint.UriScheme is not "https" or "http")
                {
                    // Codespaces only does port forwarding over HTTPS. If the protocol is not HTTP or HTTPS
                    // it cannot be forwarded because it can't intercept access to the endpoint without breaking
                    // the non-HTTP protocol to do GitHub auth.
                    continue;
                }

                var port = endpoint.AllocatedEndpoint!.Port.ToString(CultureInfo.InvariantCulture);

                JsonObject? portAttributes;
                if (!portsAttributes.TryGetPropertyValue(port, out var portAttributeNode))
                {
                    portAttributes = new JsonObject();
                    portsAttributes.Add(port, portAttributes);
                }
                else
                {
                    portAttributes = (JsonObject)portAttributeNode!;
                }

                var label = $"{resource.Name}-{endpoint.Name}";

                portAttributes["label"] = label;
                portAttributes["protocol"] = endpoint.UriScheme;
                portAttributes["onAutoForward"] = "notify";

                urlsToAnnounce.Add(endpoint.AllocatedEndpoint!.UriString);
            }
        }

        // Special case handling for the dashboard.
        

        settingsContent = settings.ToString();
        await File.WriteAllTextAsync(MachineSettingsPath, settingsContent, cancellationToken).ConfigureAwait(false);

        foreach (var urlToAnnounce in urlsToAnnounce)
        {
            _hostingLogger.LogInformation("Port forwarding: {Url}", urlToAnnounce);
        }
    }
}