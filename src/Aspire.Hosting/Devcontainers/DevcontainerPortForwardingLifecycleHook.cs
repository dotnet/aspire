// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Devcontainers.Codespaces;
using Aspire.Hosting.Eventing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Devcontainers;

internal sealed class DevcontainerPortForwardingLifecycleHook : IHostedService
{
    private readonly ILogger _hostingLogger;

    private readonly IDistributedApplicationEventing _eventing;
    private readonly IOptions<CodespacesOptions> _codespacesOptions;
    private readonly IOptions<DevcontainersOptions> _devcontainersOptions;
    private readonly IOptions<SshRemoteOptions> _sshRemoteOptions;
    private readonly DevcontainerSettingsWriter _settingsWriter;

    public DevcontainerPortForwardingLifecycleHook(
        ILoggerFactory loggerFactory,
        IDistributedApplicationEventing eventing,
        IOptions<CodespacesOptions> codespacesOptions,
        IOptions<DevcontainersOptions> devcontainersOptions,
        IOptions<SshRemoteOptions> sshRemoteOptions,
        DevcontainerSettingsWriter settingsWriter)
    {
        _hostingLogger = loggerFactory.CreateLogger("Aspire.Hosting");
        _eventing = eventing;
        _codespacesOptions = codespacesOptions;
        _devcontainersOptions = devcontainersOptions;
        _sshRemoteOptions = sshRemoteOptions;
        _settingsWriter = settingsWriter;
    }

    public async Task OnResourceEndpointsAllocatedAsync(ResourceEndpointsAllocatedEvent @event, CancellationToken cancellationToken)
    {
        if (@event.Resource is not IResourceWithEndpoints resourceWithEndpoints)
        {
            return;
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
                $"{@event.Resource.Name}-{endpoint.Name}");
        }

        await _settingsWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_devcontainersOptions.Value.IsDevcontainer && !_codespacesOptions.Value.IsCodespace && !_sshRemoteOptions.Value.IsSshRemote)
        {
            // We aren't a codespace, devcontainer, or SSH remote so there is nothing to do here.
            return Task.CompletedTask;
        }

        _eventing.Subscribe<ResourceEndpointsAllocatedEvent>(OnResourceEndpointsAllocatedAsync);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}