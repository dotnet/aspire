// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Aspire.Dashboard.Model.BrowserStorage;

public class LocalBrowserStorage : BrowserStorageBase, ILocalStorage
{
    public LocalBrowserStorage(ProtectedLocalStorage protectedLocalStorage) : base(protectedLocalStorage)
    {
    }
}

public class SessionBrowserStorage : BrowserStorageBase, ISessionStorage
{
    public SessionBrowserStorage(ProtectedLocalStorage protectedLocalStorage) : base(protectedLocalStorage)
    {
    }
}

public abstract class BrowserStorageBase : IBrowserStorage
{
    private readonly ProtectedBrowserStorage _protectedBrowserStorage;

    protected BrowserStorageBase(ProtectedBrowserStorage protectedBrowserStorage)
    {
        _protectedBrowserStorage = protectedBrowserStorage;
    }

    public async Task<StorageResult<T>> GetAsync<T>(string key)
    {
        var result = await _protectedBrowserStorage.GetAsync<T>(key).ConfigureAwait(false);
        return new StorageResult<T>(result.Success, result.Value);
    }

    public async Task SetAsync<T>(string key, T value)
    {
        await _protectedBrowserStorage.SetAsync(key, value!).ConfigureAwait(false);
    }
}
