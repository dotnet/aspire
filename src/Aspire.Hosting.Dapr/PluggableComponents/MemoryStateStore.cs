// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text;
using Dapr.PluggableComponents.Components;
using Dapr.PluggableComponents.Components.StateStore;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dapr.PluggableComponents;

internal sealed class MemoryStateStore : IStateStore
{
    private readonly ILogger<MemoryStateStore> _logger;

    private readonly IDictionary<string, string> _storage = new ConcurrentDictionary<string, string>();

    public MemoryStateStore(ILogger<MemoryStateStore> logger)
    {
        this._logger = logger;
    }

    #region IStateStore Members

    public Task DeleteAsync(StateStoreDeleteRequest request, CancellationToken cancellationToken = default)
    {
        this._logger.LogInformation("Delete request for key {key}", request.Key);

        this._storage.Remove(request.Key);

        return Task.CompletedTask;
    }

    public Task<StateStoreGetResponse?> GetAsync(StateStoreGetRequest request, CancellationToken cancellationToken = default)
    {
        this._logger.LogInformation("Get request for key {key}", request.Key);

        StateStoreGetResponse? response = null;

        if (this._storage.TryGetValue(request.Key, out var data))
        {
            response = new StateStoreGetResponse
            {
                Data = Encoding.UTF8.GetBytes(data)
            };
        }

        return Task.FromResult(response);
    }

    public Task InitAsync(MetadataRequest request, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SetAsync(StateStoreSetRequest request, CancellationToken cancellationToken = default)
    {
        this._logger.LogInformation("Set request for key {key}", request.Key);

        this._storage[request.Key] = Encoding.UTF8.GetString(request.Value.Span);

        return Task.CompletedTask;
    }

    #endregion
}
