// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dapr.PluggableComponents;

internal sealed class StateStore
{
    private readonly ILogger<StateStore> _logger;

    private readonly IDictionary<string, string> _storage = new ConcurrentDictionary<string, string>();

    public StateStore(ILogger<StateStore> logger)
    {
        this._logger = logger;
    }

    public Task DeleteAsync(string key)
    {
        this._logger.LogInformation("Delete request for key {Key}", key);

        this._storage.Remove(key);

        return Task.CompletedTask;
    }

    public Task<byte[]?> GetAsync(string key)
    {
        this._logger.LogInformation("Get request for key {Key}", key);

        byte[]? response = null;

        if (this._storage.TryGetValue(key, out var data))
        {
            response = Encoding.UTF8.GetBytes(data);
        }

        return Task.FromResult(response);
    }

    public Task SetAsync(string key, ReadOnlySpan<byte> value)
    {
        this._logger.LogInformation("Set request for key {Key}", key);

        this._storage[key] = Encoding.UTF8.GetString(value);

        return Task.CompletedTask;
    }
}
