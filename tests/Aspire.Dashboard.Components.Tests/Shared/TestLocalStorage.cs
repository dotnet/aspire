// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;

namespace Aspire.Dashboard.Components.Tests.Controls;

public sealed class TestLocalStorage : ILocalStorage
{
    public Task<LocalStorageResult<T>> GetAsync<T>(string key)
    {
        return Task.FromResult<LocalStorageResult<T>>(new LocalStorageResult<T>(Success: false, Value: default));
    }

    public Task SetAsync<T>(string key, T value)
    {
        return Task.CompletedTask;
    }
}
