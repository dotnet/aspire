// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Dashboard.Model;
using Microsoft.JSInterop;

namespace Aspire.Dashboard;

public static class ShortcutManager
{
    private static readonly ConcurrentDictionary<IGlobalKeydownListener, IGlobalKeydownListener> s_globalKeydownListenerComponents = [];

    public static void AddGlobalKeydownListener(IGlobalKeydownListener listener)
    {
        s_globalKeydownListenerComponents[listener] = listener;
    }

    public static void RemoveGlobalKeydownListener(IGlobalKeydownListener listener)
    {
        s_globalKeydownListenerComponents.Remove(listener, out _);
    }

    [JSInvokable]
    public static Task OnGlobalKeyDown(KeyboardEventArgsWithPressedKeys args)
    {
        return Task.WhenAll(s_globalKeydownListenerComponents.Values.Select(component => component.OnPageKeyDownAsync(args)));
    }
}
