// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Localization;

namespace Aspire.Dashboard.Tests;

public sealed class TestStringLocalizer<T> : IStringLocalizer<T>
{
    public LocalizedString this[string name] => new LocalizedString(name, $"Localized:{name}");
    public LocalizedString this[string name, params object[] arguments] => new LocalizedString(name, $"Localized:{name}:" + string.Join("+", arguments));

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
}
