// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Devcontainers.Codespaces;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Devcontainers;

internal sealed class DevcontainerPortForwardingLifecycleHook : IDistributedApplicationLifecycleHook
{
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

        foreach (var resource in appModel.Resources)
        {
            if (resource.Name == KnownResourceNames.AspireDashboard)
            {
                // We don't configure the dashboard here because if we print out the URL
                // the dashboard will launch immediately but it hasn't actually started
                // which would lead to a poor experience. So we'll let the dashboard
                // URL writing logic call the helper directly.
                continue;
            }

            if (resource is not IResourceWithEndpoints resourceWithEndpoints)
            {
                continue;
            }

            foreach (var endpoint in resourceWithEndpoints.Annotations.OfType<EndpointAnnotation>())
            {
                if (_codespacesOptions.Value.IsCodespace && !(endpoint.UriScheme is "https" or "http"))
                {
                    // Codespaces only does port forwarding over HTTPS. If the protocol is not HTTP or HTTPS
                    // it cannot be forwarded because it can't intercept access to the endpoint without breaking
                    // the non-HTTP protocol to do GitHub auth.
                    continue;
                }

                // TODO: This is inefficient because we are opening the file, parsing it, updating it
                //       and writing it each time. Its like this for now beause I need to use the logic
                //       in a few places (here and when we print out the Dashboard URL) - but will need
                //       to come back and optimize this to support some kind of batching.
                await DevcontainerPortForwardingHelper.SetPortAttributesAsync(
                    endpoint.AllocatedEndpoint!.Port,
                    endpoint.UriScheme,
                    $"{resource.Name}-{endpoint.Name}",
                    cancellationToken).ConfigureAwait(false);

                _hostingLogger.LogInformation("Port forwarding: {Url}", endpoint.AllocatedEndpoint!.UriString);
            }
        }
    }
}