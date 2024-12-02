// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Aspire.Dashboard.Utils;

internal static class GlobalizationHelpers
{
    // our localization list comes from https://github.com/dotnet/arcade/blob/89008f339a79931cc49c739e9dbc1a27c608b379/src/Microsoft.DotNet.XliffTasks/build/Microsoft.DotNet.XliffTasks.props#L22
    public static HashSet<string> LocalizedCultures { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "en", "cs", "de", "es", "fr", "it", "ja", "ko", "pl", "pt-BR", "ru", "tr", "zh-Hans", "zh-Hant", // Standard cultures for compliance.
    };

    public static string[] GetSupportedCultures()
    {
        var supportedCultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
            .Where(culture => LocalizedCultures.Contains(culture.TwoLetterISOLanguageName) || LocalizedCultures.Contains(culture.Name))
            .Select(culture => culture.Name)
            .ToList();

        // Non-standard culture but it is the default in many Chinese browsers. Adding zh-CN allows OS culture customization to flow through the dashboard.
        supportedCultures.Add("zh-CN");
        return supportedCultures.ToArray();
    }

    public static bool TryGetCulture(this ISet<CultureInfo> cultureOptions, CultureInfo culture, bool matchParent, [NotNullWhen(true)] out CultureInfo? matchedCulture)
    {
       if (cultureOptions.Contains(culture))
       {
           matchedCulture = culture;
           return true;
       }

       // this doesn't work for zh as we support two different zh cultures
       if (matchParent && !StringComparers.Culture.Equals(culture.TwoLetterISOLanguageName, "zh"))
       {
           var parent = culture.Parent;
           while (!Equals(parent, parent.Parent))
           {
               if (cultureOptions.Contains(parent))
               {
                   matchedCulture = parent;
                   return true;
               }
           }
       }

       matchedCulture = null;
       return false;
    }
}
