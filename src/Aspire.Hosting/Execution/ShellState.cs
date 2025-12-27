// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Represents the immutable state of a virtual shell.
/// </summary>
/// <param name="WorkingDirectory">The current working directory.</param>
/// <param name="Environment">The environment variables.</param>
internal sealed record ShellState(
    string? WorkingDirectory,
    ImmutableDictionary<string, string?> Environment)
{
    /// <summary>
    /// Creates a new default shell state.
    /// </summary>
    public static ShellState Default { get; } = new(null, ImmutableDictionary<string, string?>.Empty);

    /// <summary>
    /// Creates a new shell state with the specified environment variable set or removed.
    /// </summary>
    /// <param name="key">The environment variable name.</param>
    /// <param name="value">The value, or null to remove the variable.</param>
    /// <returns>A new shell state with the updated environment.</returns>
    public ShellState WithEnv(string key, string? value)
    {
        var newEnv = value is null
            ? Environment.Remove(key)
            : Environment.SetItem(key, value);
        return this with { Environment = newEnv };
    }

    /// <summary>
    /// Creates a new shell state with the specified environment variables merged.
    /// </summary>
    /// <param name="vars">The environment variables to merge.</param>
    /// <returns>A new shell state with the updated environment.</returns>
    public ShellState WithEnv(IReadOnlyDictionary<string, string?> vars)
    {
        var builder = Environment.ToBuilder();
        foreach (var (key, value) in vars)
        {
            if (value is null)
            {
                builder.Remove(key);
            }
            else
            {
                builder[key] = value;
            }
        }
        return this with { Environment = builder.ToImmutable() };
    }
}
