// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

internal static class LinqExtensions
{
    public static (int index, T value) IndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        var index = 0;

        foreach (var item in source)
        {
            if (predicate(item))
            {
                return (index, item);
            }

            index++;
        }

        return (-1, default!);
    }
}
