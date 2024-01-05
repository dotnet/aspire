// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Dapr.PluggableComponents.Components;
using Dapr.PluggableComponents.Components.StateStore;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dapr.PluggableComponents;

internal sealed class MemoryStateStore : IStateStore
{
    private readonly ILogger<MemoryStateStore> _logger;

    private readonly StateStore _stateStore;

    public MemoryStateStore(ILogger<MemoryStateStore> logger, StateStore stateStore)
    {
        _logger = logger;
        _stateStore = stateStore;
    }

    #region IStateStore Members

    public Task DeleteAsync(StateStoreDeleteRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Delete request for key {key}", request.Key);

        return _stateStore.DeleteAsync(request.Key);
    }

    public async Task<StateStoreGetResponse?> GetAsync(StateStoreGetRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Get request for key {key}", request.Key);

        StateStoreGetResponse? response = null;

        var value = await _stateStore.GetKeyAsync(request.Key).ConfigureAwait(false);

        if (value is not null)
        {
            response = new StateStoreGetResponse
            {
                Data = Encoding.UTF8.GetBytes(value)
            };
        }

        return response;
    }

    public Task InitAsync(MetadataRequest request, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task SetAsync(StateStoreSetRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Set request for key {key}", request.Key);

        await _stateStore.SetAsync(request.Key, Encoding.UTF8.GetString(request.Value.Span)).ConfigureAwait(false);
    }

    #endregion
}
