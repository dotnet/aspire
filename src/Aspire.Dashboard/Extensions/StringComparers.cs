// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Extensions;

internal static class StringComparers
{
    public static StringComparer ResourceType => StringComparer.Ordinal;
    public static StringComparer UserTextSearch => StringComparer.CurrentCultureIgnoreCase;
}

internal static class StringComparisons
{
    public static StringComparison ResourceType => StringComparison.Ordinal;
    public static StringComparison UserTextSearch => StringComparison.CurrentCultureIgnoreCase;
}
