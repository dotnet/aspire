// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Model.BrowserStorage;

public class LocalBrowserStorage : BrowserStorageBase, ILocalStorage
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<LocalBrowserStorage> _logger;

    public LocalBrowserStorage(IJSRuntime jsRuntime, ProtectedLocalStorage protectedLocalStorage, ILogger<LocalBrowserStorage> logger) : base(protectedLocalStorage)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task<StorageResult<T>> GetUnprotectedAsync<T>(string key)
    {
        var json = await GetJsonAsync(key).ConfigureAwait(false);

        if (json == null)
        {
            return new StorageResult<T>(false, default);
        }

        try
        {
            return new StorageResult<T>(true, JsonSerializer.Deserialize<T>(json, s_options));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error when reading '{key}' as {typeof(T).Name} from local browser storage.");

            return new StorageResult<T>(false, default);
        }
    }

    public async Task SetUnprotectedAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value, s_options);

        await SetJsonAsync(key, json).ConfigureAwait(false);
    }

    private ValueTask SetJsonAsync(string key, string json)
        => _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);

    private ValueTask<string?> GetJsonAsync(string key)
        => _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
}
