// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// Represents a connection string.
/// </summary>
public readonly struct ConnectionString
{
    /// <summary>
    /// Initializes a new instance of <see cref="ConnectionString"/> with a name and value.
    /// </summary>
    /// <param name="name">The name of the connection string.</param>
    /// <param name="value">The value of the connection string</param>
    public ConnectionString(string name, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(value);

        Name = name;
        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ConnectionString"/> with a name.
    /// </summary>
    /// <param name="name">The name of the connection string.</param>
    public ConnectionString(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        Name = name;
        Value = null;
    }

    /// <summary>
    /// The name of the connection string.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The value of the connection string.
    /// </summary>
    public string? Value { get; }
}
