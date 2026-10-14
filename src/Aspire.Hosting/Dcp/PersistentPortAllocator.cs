// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Dcp;

internal sealed class PersistentPortAllocator
{
    private readonly object _lock = new();
    private readonly DistributedApplicationModel _model;
    private readonly IConfiguration _configuration;
    private readonly DcpOptions _options;
    private readonly ProxylessEndpointPortAllocator _allocator;
    private readonly PersistentPortAllocationStore _store;
    private readonly ILogger<PersistentPortAllocator> _logger;
    private readonly Dictionary<PersistentPortAllocationKey, int> _reservedPorts = new();
    private bool _initialized;

    public PersistentPortAllocator(
        DistributedApplicationModel model,
        IConfiguration configuration,
        IOptions<DcpOptions> options,
        ProxylessEndpointPortAllocator allocator,
        PersistentPortAllocationStore store,
        ILogger<PersistentPortAllocator> logger)
    {
        _model = model;
        _configuration = configuration;
        _options = options.Value;
        _allocator = allocator;
        _store = store;
        _logger = logger;
    }

    public bool PersistenceEnabled => _store.IsAvailable && (!_options.RandomizePorts || _options.PersistPortsInIsolatedMode);

    public int GetOrAllocatePersistentPort(PersistentPortAllocationKey key, ProtocolType protocol, int? legacyPort = null)
    {
        lock (_lock)
        {
            EnsureInitialized();

            if (PersistenceEnabled && _reservedPorts.TryGetValue(key, out var reservedPort))
            {
                return reservedPort;
            }

            if (PersistenceEnabled &&
                legacyPort is > 0 and <= 65535 &&
                _allocator.TryReservePort(legacyPort.Value, protocol))
            {
                _reservedPorts[key] = legacyPort.Value;
                _store.SetPort(key, legacyPort.Value);
                return legacyPort.Value;
            }

            var allocatedPort = _allocator.AllocatePort(protocol);
            if (PersistenceEnabled)
            {
                _reservedPorts[key] = allocatedPort;
                _store.SetPort(key, allocatedPort);
            }

            return allocatedPort;
        }
    }

    public int AllocateTransientPort(ProtocolType protocol)
    {
        lock (_lock)
        {
            EnsureInitialized();
            return _allocator.AllocatePort(protocol);
        }
    }

    public int? GetLegacyPort(PersistentPortAllocationKey key)
    {
        var legacyConfigurationKey = GetLegacyConfigurationKey(key);
        var configuredPort = _configuration[legacyConfigurationKey];
        if (configuredPort is null)
        {
            return null;
        }

        if (int.TryParse(configuredPort, NumberStyles.None, CultureInfo.InvariantCulture, out var port) &&
            PortRange.IsValidPort(port))
        {
            return port;
        }

        _logger.LogDebug("Ignoring invalid legacy persisted port value '{Port}' for {PortKey}.", configuredPort, legacyConfigurationKey);
        return null;
    }

    private static string GetLegacyConfigurationKey(PersistentPortAllocationKey key)
    {
        return $"Resources:{key.ResourceName}:{key.EndpointName}:port";
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        var activePortKeys = new Dictionary<PersistentPortAllocationKey, ProtocolType>();

        foreach (var resource in _model.Resources)
        {
            if (!resource.TryGetEndpoints(out var endpoints))
            {
                continue;
            }

            foreach (var endpoint in endpoints)
            {
                if (DcpModelUtilities.TryGetEffectiveFixedPublicPort(resource, endpoint, _options.RandomizePorts, out var fixedPublicPort))
                {
                    _allocator.ExcludePort(fixedPublicPort);
                }

                if (resource is IComputeResource &&
                    !resource.IsContainer() &&
                    EndpointAnnotation.NormalizePort(endpoint.TargetPort) is int fixedTargetPort)
                {
                    _allocator.ExcludePort(fixedTargetPort);
                }

                if (DcpModelUtilities.NeedsGeneratedPublicPort(resource, endpoint, _options.RandomizePorts))
                {
                    var publicPortKey = new PersistentPortAllocationKey(resource.Name, endpoint.Name);
                    activePortKeys[publicPortKey] = endpoint.Protocol;
                }
            }
        }

        if (PersistenceEnabled)
        {
            foreach (var allocation in _store.GetAllocations())
            {
                if (activePortKeys.TryGetValue(allocation.Key, out var protocol) &&
                    _allocator.TryReservePort(allocation.Value, protocol))
                {
                    _reservedPorts[allocation.Key] = allocation.Value;
                }
            }
        }

        _initialized = true;
    }
}
