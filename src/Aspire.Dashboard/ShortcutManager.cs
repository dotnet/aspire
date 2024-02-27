// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Aspire.Dashboard;

public sealed class ShortcutManager : IDisposable
{
    private readonly ConcurrentDictionary<IGlobalKeydownListener, IGlobalKeydownListener> _keydownListenerComponents = [];

    public void AddGlobalKeydownListener(IGlobalKeydownListener listener)
    {
        _keydownListenerComponents[listener] = listener;
    }

    public void RemoveGlobalKeydownListener(IGlobalKeydownListener listener)
    {
        _keydownListenerComponents.Remove(listener, out _);
    }

    [JSInvokable]
    public Task OnGlobalKeyDown(KeyboardEventArgs args)
    {
        return Task.WhenAll(_keydownListenerComponents.Values.Select(component => component.OnPageKeyDownAsync(args)));
    }

    public void Dispose()
    {
        _keydownListenerComponents.Clear();
    }
}
