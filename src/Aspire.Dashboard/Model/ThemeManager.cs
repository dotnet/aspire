// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public sealed class ThemeManager
{
    public const float DarkThemeLuminance = 0.15f;
    public const float LightThemeLuminance = 0.95f;
    public const string ThemeSettingSystem = "System";
    public const string ThemeSettingDark = "Dark";
    public const string ThemeSettingLight = "Light";

    private readonly object _lock = new object();
    private readonly List<ModelSubscription> _subscriptions = new List<ModelSubscription>();

    /// <summary>
    /// Note: This won't have a valid value until it has been changed.
    /// If there is a reason to get the theme before changes then the ThemeManager will need to be improved.
    /// </summary>
    public string? Theme { get; private set; }

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
