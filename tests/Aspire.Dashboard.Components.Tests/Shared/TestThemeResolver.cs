// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;

namespace Aspire.Dashboard.Components.Tests.Shared;

public sealed class TestThemeResolver : IThemeResolver
{
    public string EffectiveTheme { get; set; } = ThemeManager.ThemeSettingDark;

    public Task<ThemeSettings> GetThemeSettingsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new ThemeSettings(SelectedTheme: null, EffectiveTheme: EffectiveTheme));
    }
}
