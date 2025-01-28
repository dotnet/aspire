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

    public LocalBrowserStorage(IJSRuntime jsRuntime, ProtectedLocalStorage protectedLocalStorage, ILogger<LocalBrowserStorage> logger) : base(protectedLocalStorage, logger)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<StorageResult<TValue>> GetUnprotectedAsync<TValue>(string key)
    {
        var json = await GetJsonAsync(key).ConfigureAwait(false);

        if (json == null)
        {
            return new StorageResult<TValue>(false, default);
        }

        try
        {
            return new StorageResult<TValue>(true, JsonSerializer.Deserialize<TValue>(json, s_options));
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error when reading '{Key}' as {ValueType}.", key, typeof(TValue).Name);

            return new StorageResult<TValue>(false, default);
        }
    }

    public async Task SetUnprotectedAsync<TValue>(string key, TValue value)
    {
        var json = JsonSerializer.Serialize(value, s_options);

        await SetJsonAsync(key, json).ConfigureAwait(false);
    }

    private ValueTask SetJsonAsync(string key, string json)
        => _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);

    private ValueTask<string?> GetJsonAsync(string key)
        => _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
}
