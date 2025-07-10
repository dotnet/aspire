// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting;
using Microsoft.Extensions.Configuration;

namespace Aspire.Shared;

internal static class LocaleHelpers
{
    // our localization list comes from https://github.com/dotnet/arcade/blob/89008f339a79931cc49c739e9dbc1a27c608b379/src/Microsoft.DotNet.XliffTasks/build/Microsoft.DotNet.XliffTasks.props#L22
    public static readonly string[] SupportedLocales = ["en", "cs", "de", "es", "fr", "it", "ja", "ko", "pl", "pt-BR", "ru", "tr", "zh-CN", "zh-Hans", "zh-Hant"];

    public static SetLocaleResult TrySetLocaleOverride(string localeOverride)
    {
        try
        {
            var cultureInfo = new CultureInfo(localeOverride);
            if (SupportedLocales.Contains(cultureInfo.Name) ||
                SupportedLocales.Contains(cultureInfo.TwoLetterISOLanguageName))
            {
                CultureInfo.CurrentUICulture = cultureInfo;
                CultureInfo.CurrentCulture = cultureInfo;
                CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
                CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
                return SetLocaleResult.Success;
            }

            return SetLocaleResult.UnsupportedLocale;
        }
        catch (CultureNotFoundException)
        {
            return SetLocaleResult.InvalidLocale;
        }
    }

    public static string? GetLocaleOverride(IConfiguration configuration)
    {
        var localeOverride = configuration[KnownConfigNames.LocaleOverride]
                             // also support DOTNET_CLI_UI_LANGUAGE as it's a common dotnet environment variable
                             ?? configuration[KnownConfigNames.DotnetCliUiLanguage];
        return localeOverride;
    }
}

internal enum SetLocaleResult
{
    Success,
    InvalidLocale,
    UnsupportedLocale
}
