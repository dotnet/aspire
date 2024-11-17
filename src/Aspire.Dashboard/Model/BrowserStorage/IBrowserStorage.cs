// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model.BrowserStorage;

public interface IBrowserStorage
{
    Task<StorageResult<T>> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value);
}
