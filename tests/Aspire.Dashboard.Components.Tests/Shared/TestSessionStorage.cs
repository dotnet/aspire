// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model.BrowserStorage;

namespace Aspire.Dashboard.Components.Tests.Shared;

public sealed class TestSessionStorage : ISessionStorage
{
    public Task<StorageResult<T>> GetAsync<T>(string key)
    {
        return Task.FromResult<StorageResult<T>>(new StorageResult<T>(Success: false, Value: default));
    }

    public Task SetAsync<T>(string key, T value)
    {
        return Task.CompletedTask;
    }
}
