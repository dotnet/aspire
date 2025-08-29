// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.DevTunnels;

/// <summary>
/// A minimal in-process dev tunnel "host" that simulates using the Microsoft.DevTunnels.Connections package
/// to create and expose ports. This class is used by eventing-based lifecycle handlers.
/// </summary>
internal sealed class DevTunnelInProcessHost : IAsyncDisposable
{
    private readonly DevTunnelResource _tunnel;
    private readonly ResourceLoggerService _resourceLogger;
    private readonly ResourceNotificationService _notifications;

    public DevTunnelInProcessHost(
        DevTunnelResource tunnel,
        ResourceLoggerService resourceLogger,
        ResourceNotificationService notifications)
    {
        _tunnel = tunnel;
        _resourceLogger = resourceLogger;
        _notifications = notifications;
    }

    public async Task StartAsync(IReadOnlyList<DevTunnelPortResource> ports, CancellationToken cancellationToken = default)
    {
        var log = _resourceLogger.GetLogger(_tunnel);

        // Placeholder: In a real implementation, authenticate and create the tunnel using Microsoft.DevTunnels.Connections.
        // For each port, set a deterministic public URL and optional inspect URL on the child resource and log status.

        foreach (var port in ports)
        {
            var publicUrl = BuildPublicUrlPlaceholder(port);
            var inspectUrl = port.Options.EnableInspect ? $"{publicUrl}/_inspect" : null;

            port.InspectUrl = inspectUrl;

            await _notifications.PublishUpdateAsync(port, s =>
            {
                var props = s.Properties.ToList();
                props.RemoveAll(p => p.Name == "PublicUrl" || p.Name == "InspectUrl");
                props.Add(new("PublicUrl", publicUrl));
                if (inspectUrl is not null)
                {
                    props.Add(new("InspectUrl", inspectUrl));
                }

                return s with { Properties = props };
            }, cancellationToken).ConfigureAwait(false);

            var portLogger = _resourceLogger.GetLogger(port);
            portLogger.LogInformation("Tunnel port '{Port}' exposed at: {Url}", port.Name, publicUrl);
            if (inspectUrl is not null)
            {
                portLogger.LogInformation("Inspect URL: {Url}", inspectUrl);
            }
        }
    }

    private static string BuildPublicUrlPlaceholder(DevTunnelPortResource port)
    {
        var scheme = (port.Options.Protocol?.ToLowerInvariant()) switch
        {
            "https" => "https",
            _ => "http"
        };

        var sub = $"{port.Parent.Name}-{port.TargetResource.Name}-{port.SourceEndpointName}".ToLowerInvariant();
        return $"{scheme}://{sub}.example-tunnels.dev";
    }

    public ValueTask DisposeAsync()
    {
        // Dispose SDK objects if incorporated in a real implementation.
        return ValueTask.CompletedTask;
    }
}