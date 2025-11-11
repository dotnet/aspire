// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Resources;
using Microsoft.Extensions.Localization;

namespace Aspire.Dashboard.Model.Assistant;

/// <summary>
/// A localizer for when just invariant (aka English) strings are required.
/// </summary>
public sealed class InvariantStringLocalizer<T> : IStringLocalizer<T>
{
    private readonly ResourceManager _resourceManager;

    public InvariantStringLocalizer()
    {
        var type = typeof(T);
        _resourceManager = new ResourceManager(type.FullName!, type.Assembly);
    }

    public LocalizedString this[string name]
    {
        get
        {
            var value = _resourceManager.GetString(name, CultureInfo.InvariantCulture);
            return new LocalizedString(name, value ?? string.Empty, value != null);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var value = _resourceManager.GetString(name, CultureInfo.InvariantCulture);
            return new LocalizedString(name, value != null ? string.Format(CultureInfo.InvariantCulture, value, arguments) : string.Empty, value != null);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        throw new NotImplementedException();
    }
}
