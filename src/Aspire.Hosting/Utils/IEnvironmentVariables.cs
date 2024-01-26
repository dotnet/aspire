// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Aspire;

/// <summary>
/// An abstraction over the current process's environment variables.
/// </summary>
internal interface IEnvironmentVariables
{
    /// <summary>
    /// Gets the named environment variable's value, or <see langword="null"/>.
    /// </summary>
    /// <param name="variableName">The name of the environment variable.</param>
    /// <param name="defaultValue">An optional default value to return if the environment variable is not present.</param>
    /// <returns>The named environment variable's value if present, or <see langword="null"/> if the variable is not present and no default was specified.</returns>
    [return: NotNullIfNotNull(nameof(defaultValue))]
    string? GetString(string variableName, string? defaultValue = null);
}

internal sealed class EnvironmentVariables : IEnvironmentVariables
{
    /// <summary>
    /// A cache of all queried environment variables.
    /// </summary>
    private ImmutableDictionary<string, string?> _valueByName = ImmutableDictionary<string, string?>.Empty.WithComparers(StringComparers.EnvironmentVariableName);

    [return: NotNullIfNotNull(nameof(defaultValue))]
    public string? GetString(string variableName, string? defaultValue = null)
    {
        // Environment.GetEnvironmentVariable queries the variable each time,
        // but our variables don't change during the lifetime of the process.
        // So we cache them for faster repeat lookup.
        var value = ImmutableInterlocked.GetOrAdd(ref _valueByName, key: variableName, valueFactory: Environment.GetEnvironmentVariable);

        return value ?? defaultValue;
    }
}

internal static class IEnvironmentVariablesExtensions
{
    /// <summary>
    /// Gets the named environment variable's value as a boolean.
    /// </summary>
    /// <remarks>
    /// Parses <c>true</c> and <c>false</c>, along with integer values (where non-zero is <see langword="true"/>).
    /// </remarks>
    /// <param name="env">The <see cref="IEnvironmentVariables"/> this method extends.</param>
    /// <param name="variableName">The name of the environment variable.</param>
    /// <returns>The parsed value, or <see langword="null"/> if no value exists or it couldn't be parsed.</returns>
    public static bool? GetBool(this IEnvironmentVariables env, string variableName)
    {
        var str = env.GetString(variableName);

        if (str is null or [])
        {
            return null;
        }
        else if (bool.TryParse(str, out var b))
        {
            return b;
        }
        else if (int.TryParse(str, out var i))
        {
            return i != 0;
        }

        return null;
    }

    /// <summary>
    /// Gets the named environment variable's value as a boolean.
    /// </summary>
    /// <remarks>
    /// Parses <c>true</c> and <c>false</c>, along with <c>1</c> and <c>0</c>.
    /// </remarks>
    /// <param name="env">The <see cref="IEnvironmentVariables"/> this method extends.</param>
    /// <param name="variableName">The name of the environment variable.</param>
    /// <param name="defaultValue">A default value, for when the environment variable is unspecified or white space.</param>
    /// <returns></returns>
    public static bool GetBool(this IEnvironmentVariables env, string variableName, bool defaultValue)
    {
        return env.GetBool(variableName) ?? defaultValue;
    }

    /// <summary>
    /// Parses a environment variable's value into a <see cref="Uri"/> object.
    /// </summary>
    /// <param name="env">The <see cref="IEnvironmentVariables"/> this method extends.</param>
    /// <param name="variableName">The name of the environment variable.</param>
    /// <param name="defaultValue">A default value, for when the environment variable is unspecified or white space. May be <see langword="null"/>.</param>
    /// <returns>The parsed value, or the default value if specified and parsing failed. Returns <see langword="null"/> if <paramref name="defaultValue"/> is <see langword="null"/> and parsing failed.</returns>
    /// <exception cref="InvalidOperationException">The environment variable could not be accessed, or contained incorrectly formatted data.</exception>
    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static Uri? GetUri(this IEnvironmentVariables env, string variableName, Uri? defaultValue = null)
    {
        try
        {
            var uri = env.GetString(variableName);

            if (string.IsNullOrWhiteSpace(uri))
            {
                return defaultValue;
            }
            else
            {
                return new Uri(uri, UriKind.Absolute);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error parsing URIs from environment variable '{variableName}'.", ex);
        }
    }

    /// <summary>
    /// Parses a environment variable's semicolon-delimited value into an array of <see cref="Uri"/> objects.
    /// </summary>
    /// <param name="env">The <see cref="IEnvironmentVariables"/> this method extends.</param>
    /// <param name="variableName">The name of the environment variable.</param>
    /// <param name="defaultValue">A default value, for when the environment variable is unspecified or white space. May be <see langword="null"/>.</param>
    /// <returns>The parsed values, or the default value if specified and parsing failed. Returns <see langword="null"/> if <paramref name="defaultValue"/> is <see langword="null"/> and parsing failed.</returns>
    /// <exception cref="InvalidOperationException">The environment variable could not be accessed, or contained incorrectly formatted data.</exception>
    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static Uri[]? GetUris(this IEnvironmentVariables env, string variableName, Uri? defaultValue = null)
    {
        try
        {
            var uris = env.GetString(variableName);

            if (string.IsNullOrWhiteSpace(uris))
            {
                return defaultValue switch
                {
                    not null => [defaultValue],
                    null => null
                };
            }
            else
            {
                return uris
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(url => new Uri(url, UriKind.Absolute))
                    .ToArray();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error parsing URIs from environment variable '{variableName}'.", ex);
        }
    }
}
