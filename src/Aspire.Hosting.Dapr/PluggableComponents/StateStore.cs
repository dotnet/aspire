// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dapr.PluggableComponents;

internal sealed class StateStore
{
    private readonly ILogger<StateStore> _logger;

    private readonly IDictionary<string, string> _storage = new ConcurrentDictionary<string, string>();

    public StateStore(ILogger<StateStore> logger)
    {
        _logger = logger;
    }

    public Task DeleteAsync(string key)
    {
        _logger.LogInformation("Delete request for key {Key}", key);

        _storage.Remove(key);

        return Task.CompletedTask;
    }

    public Task<string?> GetKeyAsync(string key)
    {
        _logger.LogInformation("Get request for key {Key}", key);

        _storage.TryGetValue(key, out var value);

        return Task.FromResult(value);
    }

    public Task<string[]> GetKeysAsync()
    {
        return Task.FromResult(_storage.Keys.ToArray());
    }

    public Task SetAsync(string key, string value)
    {
        _logger.LogInformation("Set request for key {Key}", key);

        _storage[key] = value;

        return Task.CompletedTask;
    }
}
