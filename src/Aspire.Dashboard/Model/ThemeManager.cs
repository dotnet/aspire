// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public sealed class ThemeManager
{
    public const string ThemeSettingSystem = "System";
    public const string ThemeSettingDark = "Dark";
    public const string ThemeSettingLight = "Light";

    private readonly object _lock = new object();
    private readonly List<ModelSubscription> _subscriptions = new List<ModelSubscription>();

    /// <summary>
    /// The actual theme key (null, System, Dark, Light) set by the user.
    /// </summary>
    public string? Theme { get; private set; }

    /// <summary>
    /// The effective theme, from app-theme.js, which is the theme that is actually applied to the browser window. If the set
    /// theme is System or null, this will return the evaluation of the system theme.
    /// Set after applying the theme in MainLayout
    /// </summary>
    public string? EffectiveTheme { get; internal set; }

    public IDisposable OnThemeChanged(Func<Task> callback)
    {
        lock (_lock)
        {
            var subscription = new ModelSubscription(callback, RemoveSubscription);
            _subscriptions.Add(subscription);
            return subscription;
        }
    }

    private void RemoveSubscription(ModelSubscription subscription)
    {
        lock (_lock)
        {
            _subscriptions.Remove(subscription);
        }
    }

    public async Task RaiseThemeChangedAsync(string theme)
    {
        Theme = theme;

        if (_subscriptions.Count == 0)
        {
            return;
        }

        ModelSubscription[] subscriptions;
        lock (_lock)
        {
            subscriptions = _subscriptions.ToArray();
        }

        foreach (var subscription in subscriptions)
        {
            await subscription.ExecuteAsync().ConfigureAwait(false);
        }
    }
}
