// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model.BrowserStorage;

public interface ILocalStorage : IBrowserStorage
{
    Task<StorageResult<T>> GetUnprotectedAsync<T>(string key);
    Task SetUnprotectedAsync<T>(string key, T value);
}
