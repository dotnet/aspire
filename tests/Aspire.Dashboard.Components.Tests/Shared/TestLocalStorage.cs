// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model.BrowserStorage;

namespace Aspire.Dashboard.Components.Tests.Shared;

public sealed class TestLocalStorage : ILocalStorage
{
    public Task<StorageResult<T>> GetAsync<T>(string key)
    {
        return Task.FromResult(new StorageResult<T>(Success: false, Value: default));
    }

    public Task<StorageResult<T>> GetUnprotectedAsync<T>(string key)
    {
        return Task.FromResult(new StorageResult<T>(Success: false, Value: default));
    }

    public Task SetAsync<T>(string key, T value)
    {
        return Task.CompletedTask;
    }

    public Task SetUnprotectedAsync<T>(string key, T value)
    {
        return Task.CompletedTask;
    }
}
