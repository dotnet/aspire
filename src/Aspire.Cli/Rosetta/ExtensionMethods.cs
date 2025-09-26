// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace Aspire.Cli.Rosetta;

internal static class ExtensionMethods
{
    extension(ArgumentException)
    {
        public static void ThrowIfNotReflectionOnly(Assembly assembly, [CallerArgumentExpression(nameof(assembly))] string? paramName = null)
        {
            if (!assembly.ReflectionOnly)
            {
                throw new ArgumentException("Expected a ReflectionOnly assembly", paramName);
            }
        }

        public static void ThrowIfNotReflectionOnly(Type type, [CallerArgumentExpression(nameof(type))] string? paramName = null)
        {
            if (!type.Assembly.ReflectionOnly)
            {
                throw new ArgumentException("Expected a ReflectionOnly type", paramName);
            }
        }
    }
}
