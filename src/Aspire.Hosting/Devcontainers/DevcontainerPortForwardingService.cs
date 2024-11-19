// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Json.More;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Devcontainers;

internal sealed class DevcontainerPortForwardingService(ILogger<DevcontainerPortForwardingService> logger, IOptions<DevcontainersOptions> options, ResourceNotificationService resourceNotificationService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.IsDevcontainer)
        {
            logger.LogTrace("Not running in local Devcontainer, skipping auto-port forwarding.");
            return;
        }

        var forwardedPorts = new List<int>();

        do
        {
            try
            {
                var resourceEvents = resourceNotificationService.WatchAsync(stoppingToken);

                await foreach (var resourceEvent in resourceEvents.ConfigureAwait(false))
                {
                    if (resourceEvent.Resource.TryGetEndpoints(out var endpoints))
                    {
                        foreach (var endpoint in endpoints.Where(e => e.UriScheme is "http" or "https" && e.AllocatedEndpoint is not null))
                        {
                            var machineSettingsPath = "/home/vscode/.vscode-server/data/Machine/settings.json";
                            var jsonString = await File.ReadAllTextAsync(machineSettingsPath, stoppingToken).ConfigureAwait(false);
                            using var jsonDocument = JsonDocument.Parse(jsonString);

                            if (jsonDocument.RootElement.TryGetProperty("remote.portsAttributes", out var portsAttributesElement))
                            {
                                var propertyName = endpoint.AllocatedEndpoint!.Port.ToString(CultureInfo.InvariantCulture);
                                if (!portsAttributesElement.TryGetProperty(propertyName, out _))
                                {
                                    logger.LogInformation("Property {PropertyName} does not exist in portsAttributes.", propertyName);

                                    var node = JsonNode.Parse(
                                        $$"""
                                        {
                                            "protocol": "{{endpoint.AllocatedEndpoint.Port}}",
                                            "label": "{{resourceEvent.Resource.Name}}-{{endpoint.Name}}",
                                            "onAutoForward": "notify"
                                        }
                                        """);
                                    portsAttributesElement.AsNode()!.AsObject().Add(propertyName, node);
                                }
                            }
                            // Process the JsonDocument as needed

                            using var stream = new FileStream(machineSettingsPath, FileMode.Create);
                            using var jsonWriter = new Utf8JsonWriter(stream);
                            jsonDocument.WriteTo(jsonWriter);

                            logger.LogInformation("Auto-forwarding: {Uri}", endpoint.AllocatedEndpoint!.UriString);
                        }
                    }

                    logger.LogInformation("Pretend we are auto forwarding.");
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // When debugging sometimes we'll get cancelled here but we don't want
                // to tear down the loop. We only want to crash out when the service's
                // cancellation token is signaled.
                logger.LogTrace(ex, "Devcontainer port forwarding loop threw an exception but was ignored.");
            }
        } while (!stoppingToken.IsCancellationRequested);
    }
}
