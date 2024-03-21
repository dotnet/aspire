// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Extensions;

internal static class FluentUIExtensions
{
    public static Dictionary<string, object> GetClipboardCopyAdditionalAttributes(string? text, string? precopy, string? postcopy, params (string Attribute, object Value)[] additionalAttributes)
    {
        var attributes = new Dictionary<string, object>
        {
            { "data-text", text ?? string.Empty },
            { "data-precopy", precopy ?? string.Empty },
            { "data-postcopy", postcopy ?? string.Empty },
            { "onclick", $"buttonCopyTextToClipboard(this)" }
        };

        foreach (var (attribute, value) in additionalAttributes)
        {
            attributes.Add(attribute, value);
        }

        return attributes;
    }
}
