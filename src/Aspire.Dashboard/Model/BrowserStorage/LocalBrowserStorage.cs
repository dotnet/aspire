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

    public LocalBrowserStorage(IJSRuntime jsRuntime, ProtectedLocalStorage protectedLocalStorage) : base(protectedLocalStorage)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<StorageResult<T>> GetUnprotectedAsync<T>(string key)
    {
        var json = await GetJsonAsync(key).ConfigureAwait(false);

        return json == null ?
            new StorageResult<T>(false, default) :
            new StorageResult<T>(true, JsonSerializer.Deserialize<T>(json, s_options));
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
