// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Utils;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Model;

public interface IEffectiveThemeResolver
{
    Task<string> GetEffectiveThemeAsync(CancellationToken cancellationToken);
}

public sealed class BrowserEffectiveThemeResolver(IJSRuntime jsRuntime) : IEffectiveThemeResolver, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;
    private IJSObjectReference? _jsModule;

    public async Task<string> GetEffectiveThemeAsync(CancellationToken cancellationToken)
    {
        if (_jsModule == null)
        {
            _jsModule = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "/js/app-theme.js").ConfigureAwait(false);
        }

        return await _jsModule.InvokeAsync<string>("getCurrentTheme", cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await JSInteropHelpers.SafeDisposeAsync(_jsModule).ConfigureAwait(false);
    }
}

public sealed class ThemeManager
{
    public const string ThemeSettingSystem = "System";
    public const string ThemeSettingDark = "Dark";
    public const string ThemeSettingLight = "Light";

    private readonly object _lock = new object();
    private readonly List<ModelSubscription> _subscriptions = new List<ModelSubscription>();
    private readonly IEffectiveThemeResolver _effectiveThemeResolver;
    private string? _effectiveTheme;

    public ThemeManager(IEffectiveThemeResolver effectiveThemeResolver)
    {
        _effectiveThemeResolver = effectiveThemeResolver;
    }

    /// <summary>
    /// The actual theme key (null, System, Dark, Light) set by the user.
    /// </summary>
    public string? Theme { get; private set; }

    /// <summary>
    /// The effective theme, from app-theme.js, which is the theme that is actually applied to the browser window.
    /// To ensure the effective theme is loaded from the browser, call <see cref="EnsureEffectiveThemeAsync"/> before accessing.
    /// </summary>
    public string EffectiveTheme
    {
        get
        {
            if (_effectiveTheme == null)
            {
                throw new InvalidOperationException("EffectiveTheme hasn't been set.");
            }

            return _effectiveTheme;
        }
        set => _effectiveTheme = value;
    }

    public async Task EnsureEffectiveThemeAsync()
    {
        if (_effectiveTheme == null)
        {
            _effectiveTheme = await _effectiveThemeResolver.GetEffectiveThemeAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

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
