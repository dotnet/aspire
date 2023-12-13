// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Aspire.Dashboard.Components.Pages;

public interface IMemoryPage<in T>
{
    ProtectedSessionStorage ProtectedSessionStore { get; set; }
    NavigationManager NavigationManager { get; set; }

    string GetNavigationUrl(T state);

    public void NavigateTo(T state)
    {
        NavigationManager.NavigateTo(GetNavigationUrl(state));
    }
}
