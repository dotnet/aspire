// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire;

// NOTE unit tests enforce that these two classes are kept in sync

internal static class StringComparers
{
    public static StringComparer ResourceName => StringComparer.OrdinalIgnoreCase;
    public static StringComparer ResourceState => StringComparer.OrdinalIgnoreCase;
    public static StringComparer EndpointAnnotationName => StringComparer.OrdinalIgnoreCase;
    public static StringComparer ResourceType => StringComparer.Ordinal;
    public static StringComparer ResourcePropertyName => StringComparer.Ordinal;
    public static StringComparer ResourceOwnerName => StringComparer.Ordinal;
    public static StringComparer ResourceOwnerKind => StringComparer.Ordinal;
    public static StringComparer ResourceOwnerUid => StringComparer.Ordinal;
    public static StringComparer UserTextSearch => StringComparer.CurrentCultureIgnoreCase;
    public static StringComparer EnvironmentVariableName => StringComparer.InvariantCultureIgnoreCase;
    public static StringComparer UrlPath => StringComparer.OrdinalIgnoreCase;
    public static StringComparer UrlHost => StringComparer.OrdinalIgnoreCase;
    public static StringComparer HtmlAttribute => StringComparer.Ordinal;
    public static StringComparer GridColumn => StringComparer.Ordinal;
    public static StringComparer OtlpAttribute => StringComparer.Ordinal;
}

internal static class StringComparisons
{
    public static StringComparison ResourceName => StringComparison.OrdinalIgnoreCase;
    public static StringComparison ResourceState => StringComparison.OrdinalIgnoreCase;
    public static StringComparison EndpointAnnotationName => StringComparison.OrdinalIgnoreCase;
    public static StringComparison ResourceType => StringComparison.Ordinal;
    public static StringComparison ResourcePropertyName => StringComparison.Ordinal;
    public static StringComparison ResourceOwnerName => StringComparison.Ordinal;
    public static StringComparison ResourceOwnerKind => StringComparison.Ordinal;
    public static StringComparison ResourceOwnerUid => StringComparison.Ordinal;
    public static StringComparison UserTextSearch => StringComparison.CurrentCultureIgnoreCase;
    public static StringComparison EnvironmentVariableName => StringComparison.InvariantCultureIgnoreCase;
    public static StringComparison UrlPath => StringComparison.OrdinalIgnoreCase;
    public static StringComparison UrlHost => StringComparison.OrdinalIgnoreCase;
    public static StringComparison HtmlAttribute => StringComparison.Ordinal;
    public static StringComparison GridColumn => StringComparison.Ordinal;
    public static StringComparison OtlpAttribute => StringComparison.Ordinal;
}
