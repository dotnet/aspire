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
    private readonly DevcontainerSettingsWriter _settingsWriter;

    public DevcontainerPortForwardingLifecycleHook(ILoggerFactory loggerFactory, IOptions<CodespacesOptions> codespacesOptions, IOptions<DevcontainersOptions> devcontainersOptions, DevcontainerSettingsWriter settingsWriter)
    {
        _hostingLogger = loggerFactory.CreateLogger("Aspire.Hosting");
        _codespacesOptions = codespacesOptions;
        _devcontainersOptions = devcontainersOptions;
        _settingsWriter = settingsWriter;
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

                _settingsWriter.AddPortForward(
                    endpoint.AllocatedEndpoint!.UriString,
                    endpoint.AllocatedEndpoint!.Port,
                    endpoint.UriScheme,
                    $"{resource.Name}-{endpoint.Name}");
            }
        }

        await _settingsWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}