// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Aspire.Dashboard.Components.Pages;

public interface IMemoryPage<in T>
{
    string MemoryKey { get; }
    ProtectedSessionStorage ProtectedSessionStore { get; }
    NavigationManager NavigationManager { get; }

    string GetNavigationUrl(T state);

    public void NavigateTo(T state)
    {
        NavigationManager.NavigateTo(GetNavigationUrl(state));
    }

    public async Task NavigateToCurrentStateIfSetAsync()
    {
        var result = await ProtectedSessionStore.GetAsync<T>(MemoryKey);
        if (result is { Success: true, Value: not null })
        {
            NavigateTo(result.Value);
        }
    }
}
