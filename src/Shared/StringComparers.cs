// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire;

internal static class StringComparers
{
    public static StringComparer ResourceName => StringComparer.OrdinalIgnoreCase;
    public static StringComparer EndpointAnnotationName => StringComparer.OrdinalIgnoreCase;
    public static StringComparer ResourceType => StringComparer.Ordinal;
    public static StringComparer ResourcePropertyName => StringComparer.Ordinal;
    public static StringComparer UserTextSearch => StringComparer.CurrentCultureIgnoreCase;
    public static StringComparer EnvironmentVariableName => StringComparer.InvariantCultureIgnoreCase;
    public static StringComparer UrlPath => StringComparer.OrdinalIgnoreCase;
}

internal static class StringComparisons
{
    public static StringComparison ResourceName => StringComparison.OrdinalIgnoreCase;
    public static StringComparison EndpointAnnotationName => StringComparison.OrdinalIgnoreCase;
    public static StringComparison ResourceType => StringComparison.Ordinal;
    public static StringComparison ResourcePropertyName => StringComparison.Ordinal;
    public static StringComparison UserTextSearch => StringComparison.CurrentCultureIgnoreCase;
    public static StringComparison EnvironmentVariableName => StringComparison.InvariantCultureIgnoreCase;
    public static StringComparison UrlPath => StringComparison.OrdinalIgnoreCase;
}
