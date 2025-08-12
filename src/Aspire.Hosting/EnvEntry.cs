// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// Represents an entry from an environment (.env) file.
/// </summary>
public sealed class EnvEntry
{
    /// <summary>
    /// Initializes a new instance of <see cref="EnvEntry"/>.
    /// </summary>
    /// <param name="key">The environment variable key.</param>
    /// <param name="value">The environment variable value.</param>
    /// <param name="comment">The comment associated with this entry.</param>
    public EnvEntry(string key, string? value, string? comment)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        
        Key = key;
        Value = value;
        Comment = comment;
    }

    /// <summary>
    /// Gets the environment variable key.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the environment variable value.
    /// </summary>
    public string? Value { get; }

    /// <summary>
    /// Gets the comment associated with this entry from the .env file.
    /// </summary>
    public string? Comment { get; }
}