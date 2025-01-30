// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model.BrowserStorage;

public interface IBrowserStorage
{
    Task<StorageResult<TValue>> GetAsync<TValue>(string key);
    Task SetAsync<TValue>(string key, TValue value);
}
