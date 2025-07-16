// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETFRAMEWORK

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System;

internal static partial class FxPolyfillArgumentNullException
{
    extension(ArgumentNullException)
    {
        public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument is null)
            {
                Throw(paramName);
            }
        }
    }

    [DoesNotReturn]
    internal static void Throw(string? paramName) => throw new ArgumentNullException(paramName);
}

#endif
