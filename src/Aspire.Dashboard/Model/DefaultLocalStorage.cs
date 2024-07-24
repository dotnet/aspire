// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Aspire.Dashboard.Model;

public class DefaultLocalStorage : ILocalStorage
{
    private readonly ProtectedLocalStorage _protectedBrowserStorage;

    public DefaultLocalStorage(ProtectedLocalStorage protectedBrowserStorage)
    {
        _protectedBrowserStorage = protectedBrowserStorage;
    }

    public async Task<LocalStorageResult<T>> GetAsync<T>(string key)
    {
        var result = await _protectedBrowserStorage.GetAsync<T>(key).ConfigureAwait(false);
        return new LocalStorageResult<T>(result.Success, result.Value);
    }

    public async Task SetAsync<T>(string key, T value)
    {
        await _protectedBrowserStorage.SetAsync(key, value!).ConfigureAwait(false);
    }
}
