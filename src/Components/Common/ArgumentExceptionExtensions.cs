// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Aspire;

internal static class ArgumentExceptionExtensions
{
    /// <summary>
    /// Adds a check Throw ArgumentException if argument is null or empty.
    /// And return argument
    /// </summary>
    /// <param name="argument"></param>
    /// <param name="paramName"></param>
    /// <returns>argument not null</returns>
    public static string ThrowIfNullOrEmpty(
        [NotNull] this string? argument,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(argument, paramName);
        return argument;
    }

    public static string[] ThrowIfNullOrContainsIsNullOrEmpty(
        [NotNull] this string[] args,
        [CallerArgumentExpression(nameof(args))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(args, paramName);
        foreach (var arg in args)
        {
            if (string.IsNullOrEmpty(arg))
            {
                var values = string.Join(", ", args);
                if (arg is null)
                {
                    throw new ArgumentNullException(paramName, $"Array params contains null item: [{values}]");
                }
                throw new ArgumentException($"Array params contains empty item: [{values}]", paramName);
            }
        }
        return args;
    }
}
