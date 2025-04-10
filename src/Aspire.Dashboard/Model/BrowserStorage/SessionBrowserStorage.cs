// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Aspire.Dashboard.Model.BrowserStorage;

public class SessionBrowserStorage : BrowserStorageBase, ISessionStorage
{
    public SessionBrowserStorage(ProtectedSessionStorage protectedSessionStorage, ILogger<SessionBrowserStorage> logger) : base(protectedSessionStorage, logger)
    {
    }

    public override async Task DeleteAsync(string key)
    {
        await _protectedBrowserStorage.DeleteAsync(key).ConfigureAwait(false);
    }
}
