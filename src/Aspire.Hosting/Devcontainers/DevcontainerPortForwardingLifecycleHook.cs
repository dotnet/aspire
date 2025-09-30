// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Devcontainers.Codespaces;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Devcontainers;

internal sealed class DevcontainerPortForwardingLifecycleHook : IDistributedApplicationLifecycleHook
{
    private readonly IDistributedApplicationEventing _eventing;
    private readonly ILogger _hostingLogger;
    private readonly IOptions<CodespacesOptions> _codespacesOptions;
    private readonly IOptions<DevcontainersOptions> _devcontainersOptions;
    private readonly IOptions<SshRemoteOptions> _sshRemoteOptions;
    private readonly DevcontainerSettingsWriter _settingsWriter;

    public DevcontainerPortForwardingLifecycleHook(IDistributedApplicationEventing eventing, ILoggerFactory loggerFactory, IOptions<CodespacesOptions> codespacesOptions, IOptions<DevcontainersOptions> devcontainersOptions, IOptions<SshRemoteOptions> sshRemoteOptions, DevcontainerSettingsWriter settingsWriter)
    {
        _eventing = eventing;
        _hostingLogger = loggerFactory.CreateLogger("Aspire.Hosting");
        _codespacesOptions = codespacesOptions;
        _devcontainersOptions = devcontainersOptions;
        _sshRemoteOptions = sshRemoteOptions;
        _settingsWriter = settingsWriter;
    }

    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {        
        if (!_devcontainersOptions.Value.IsDevcontainer && !_codespacesOptions.Value.IsCodespace && !_sshRemoteOptions.Value.IsSshRemote)
        {
            // We aren't a codespace, devcontainer, or SSH remote so there is nothing to do here.
            return Task.CompletedTask;
        }

        _eventing.Subscribe<ResourceEndpointsAllocatedEvent>((evt, cancellationToken) =>
        {
            var resource = evt.Resource;

            foreach (var endpoint in resource.Annotations.OfType<EndpointAnnotation>())
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

            return Task.CompletedTask;
        });

        return Task.CompletedTask;
    }
}