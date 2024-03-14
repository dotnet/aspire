// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Dashboard.Model;
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
    public Task OnGlobalKeyDown(AspireKeyboardShortcut shortcut)
    {
        var componentsSubscribedToShortcut =
            _keydownListenerComponents.Values.Where(component => component.SubscribedShortcuts.Contains(shortcut));

        return Task.WhenAll(componentsSubscribedToShortcut.Select(component => component.OnPageKeyDownAsync(shortcut)));
    }

    public void Dispose()
    {
        _keydownListenerComponents.Clear();
    }
}
