// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Aspire.Hosting;

internal sealed class LocaleOverrideContext
{
    public string? LocaleOverride { get; set; }
    public string? OverrideErrorMessage { get; set; }

    public CultureInfo? OriginalCurrentUICulture { get; set;}
    public CultureInfo? OriginalCurrentCulture { get; set; }
    public CultureInfo? OriginalDefaultThreadCurrentCulture { get; set; }
    public CultureInfo? OriginalDefaultThreadCurrentUICulture { get; set; }
}
