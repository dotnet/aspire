// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Aspire.Elastic.Clients.Elasticsearch;

internal class Guard
{
    //
    // Summary:
    //     Throws an System.ArgumentNullException if argument is null.
    //
    // Parameters:
    //   argument:
    //     The reference type argument to validate as non-null.
    //
    //   throwOnEmptyString:
    //     Only applicable to strings.
    //
    //   paramName:
    //     The name of the parameter with which argument corresponds.
    public static T ThrowIfNull<T>([NotNull] T? argument, bool throwOnEmptyString = false, [CallerArgumentExpression("argument")] string? paramName = null) where T : class
    {
        ArgumentNullException.ThrowIfNull(argument, paramName);
        if (throwOnEmptyString && argument is string value && string.IsNullOrEmpty(value))
        {
            throw new ArgumentNullException(paramName);
        }

        return argument;
    }
}
