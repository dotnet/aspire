// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#nullable enable

// Note: When implicit usings are enabled in a project this file will generate
// warnings/errors without this suppression.
#pragma warning disable IDE0005 // Using directive is unnecessary.

// Note: For some targets this file will contain more than one type/namespace.
#pragma warning disable IDE0161 // Convert to file-scoped namespace

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

#if !NET6_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    /// <summary>Allows capturing of the expressions passed to a method.</summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class CallerArgumentExpressionAttribute : Attribute
    {
        public CallerArgumentExpressionAttribute(string parameterName)
        {
            this.ParameterName = parameterName;
        }

        public string ParameterName { get; }
    }
}
#endif

#if !NET6_0_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>Specifies that an output is not <see langword="null"/> even if
    /// the corresponding type allows it. Specifies that an input argument was
    /// not <see langword="null"/> when the call returns.</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false)]
    internal sealed class NotNullAttribute : Attribute
    {
    }
}
#endif

namespace OpenTelemetry.Internal
{
    /// <summary>
    /// Methods for guarding against exception throwing values.
    /// </summary>
    internal static class Guard
    {
        /// <summary>
        /// Throw an exception if the value is null.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="paramName">The parameter name to use in the thrown exception.</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNull([NotNull] object? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            if (value is null)
            {
                throw new ArgumentNullException(paramName, "Must not be null");
            }
        }

        /// <summary>
        /// Throw an exception if the value is null or empty.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="paramName">The parameter name to use in the thrown exception.</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNullOrEmpty([NotNull] string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
#pragma warning disable CS8777 // Parameter must have a non-null value when exiting.
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Must not be null or empty", paramName);
            }
        }
#pragma warning restore CS8777 // Parameter must have a non-null value when exiting.

        /// <summary>
        /// Throw an exception if the value is null or whitespace.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="paramName">The parameter name to use in the thrown exception.</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNullOrWhitespace([NotNull] string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
#pragma warning disable CS8777 // Parameter must have a non-null value when exiting.
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Must not be null or whitespace", paramName);
            }
        }
#pragma warning restore CS8777 // Parameter must have a non-null value when exiting.

        /// <summary>
        /// Throw an exception if the value is zero.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="message">The message to use in the thrown exception.</param>
        /// <param name="paramName">The parameter name to use in the thrown exception.</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfZero(int value, string message = "Must not be zero", [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            if (value == 0)
            {
                throw new ArgumentException(message, paramName);
            }
        }

        /// <summary>
        /// Throw an exception if the value is not considered a valid timeout.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="paramName">The parameter name to use in the thrown exception.</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfInvalidTimeout(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            ThrowIfOutOfRange(value, paramName, min: Timeout.Infinite, message: $"Must be non-negative or '{nameof(Timeout)}.{nameof(Timeout.Infinite)}'");
        }

        /// <summary>
        /// Throw an exception if the value is not within the given range.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="paramName">The parameter name to use in the thrown exception.</param>
        /// <param name="min">The inclusive lower bound.</param>
        /// <param name="max">The inclusive upper bound.</param>
        /// <param name="minName">The name of the lower bound.</param>
        /// <param name="maxName">The name of the upper bound.</param>
        /// <param name="message">An optional custom message to use in the thrown exception.</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfOutOfRange(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null, int min = int.MinValue, int max = int.MaxValue, string? minName = null, string? maxName = null, string? message = null)
        {
            Range(value, paramName, min, max, minName, maxName, message);
        }

        /// <summary>
        /// Throw an exception if the value is not within the given range.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="paramName">The parameter name to use in the thrown exception.</param>
        /// <param name="min">The inclusive lower bound.</param>
        /// <param name="max">The inclusive upper bound.</param>
        /// <param name="minName">The name of the lower bound.</param>
        /// <param name="maxName">The name of the upper bound.</param>
        /// <param name="message">An optional custom message to use in the thrown exception.</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfOutOfRange(double value, [CallerArgumentExpression(nameof(value))] string? paramName = null, double min = double.MinValue, double max = double.MaxValue, string? minName = null, string? maxName = null, string? message = null)
        {
            Range(value, paramName, min, max, minName, maxName, message);
        }

        /// <summary>
        /// Throw an exception if the value is not of the expected type.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="paramName">The parameter name to use in the thrown exception.</param>
        /// <typeparam name="T">The type attempted to convert to.</typeparam>
        /// <returns>The value casted to the specified type.</returns>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ThrowIfNotOfType<T>([NotNull] object? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            if (value is not T result)
            {
                throw new InvalidCastException($"Cannot cast '{paramName}' from '{value?.GetType().ToString() ?? "null"}' to '{typeof(T)}'");
            }

            return result;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Range<T>(T value, string? paramName, T min, T max, string? minName, string? maxName, string? message)
            where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            {
                var minMessage = minName != null ? $": {minName}" : string.Empty;
                var maxMessage = maxName != null ? $": {maxName}" : string.Empty;
                var exMessage = message ?? string.Format(
                    CultureInfo.InvariantCulture,
                    "Must be in the range: [{0}{1}, {2}{3}]",
                    min,
                    minMessage,
                    max,
                    maxMessage);
                throw new ArgumentOutOfRangeException(paramName, value, exMessage);
            }
        }
    }
}
