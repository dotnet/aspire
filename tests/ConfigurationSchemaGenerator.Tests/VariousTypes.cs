// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Aspire;
using ConfigurationSchemaGenerator.Tests;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

[assembly: ConfigurationSchema("ExampleComponent", typeof(ExampleSettings))]

namespace ConfigurationSchemaGenerator.Tests;

/// <summary>
/// Example settings.
/// </summary>
public record ExampleSettings
{
    /// <summary>
    /// A value of type <see cref="bool"/>.
    ///
    /// Blank lines are preserved.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool PropertyOfBool { get; set; } = true;

    /// <summary>
    /// A value of type <see cref="double"/>.
    /// </summary>
    [ConfigurationKeyName("PropertyOfDoubleWithAlternateName")]
    public double? PropertyOfDouble { get; set; }

    /// <summary>
    /// A value of type <see cref="string"/>.
    /// </summary>
    public string PropertyOfString { get; set; } = string.Empty;

    /// <summary>
    /// A value of type <see cref="T:byte[]"/>, or a base64-encoded <see cref="string"/>.
    /// </summary>
    public byte[] PropertyOfByteArray { get; set; } = [];

    /// <summary>
    /// A value of type <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan PropertyOfTimeSpan { get; set; }

    /// <summary>
    /// A value of type <see cref="Guid"/>.
    /// </summary>
    public Guid PropertyOfGuid { get; set; }

    /// <summary>
    /// A value of type <see cref="Uri"/>.
    /// </summary>
    public Uri? PropertyOfUri { get; set; }

    /// <summary>
    /// A value of type <see cref="Enum"/>.
    /// </summary>
    public ListSortDirection PropertyOfEnum { get; set; }

    /// <summary>
    /// A value of type <see cref="object"/> (free-format).
    /// </summary>
    public object? PropertyOfObject { get; set; }

    /// <summary>
    /// A value of type <see cref="Action"/> (ignored).
    /// </summary>
    public Action? PropertyOfAction { get; set; }

    /// <summary>
    /// A <see cref="ICollection{T}"/> containing <see cref="int"/> elements.
    /// </summary>
    public ICollection<int> PropertyOfIntCollection { get; } = [];

    /// <summary>
    /// A <see cref="IDictionary{TKey,TValue}"/> of <see cref="string"/> to <see cref="TimeSpan"/>.
    /// </summary>
    public IDictionary<string, TimeSpan> PropertyOfStringToTimeSpan { get; } = new Dictionary<string, TimeSpan>();

    /// <summary>
    /// A value of type <see cref="DefaultSettings"/>.
    /// </summary>
    public DefaultSettings? PropertyOfDefaultSettings { get; } = new();

    /// <summary>
    /// A <see cref="IDictionary{TKey,TValue}"/> of <see cref="string"/> to <see cref="DefaultSettings"/>.
    /// </summary>
    public IDictionary<string, DefaultSettings> PropertyOfStringToDefaultSettings { get; } = new Dictionary<string, DefaultSettings>();

    /// <summary>
    /// A recursive data structure (preserved, but without schema)
    /// </summary>
    public TreeElement? Tree { get; set; }
}

/// <summary>
/// Nested settings.
/// </summary>
public record DefaultSettings : IJsonLineInfo
{
    /// <summary>
    /// Gets or sets the value.
    /// <para>
    /// Should never be <c>null</c>, and <b>not</b> longer than 100 characters.
    /// </para>
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// A simple read-only property (ignored).
    /// </summary>
    public bool HasParent => Parent != null;

    /// <summary>
    /// A circular reference (preserved, but without schema).
    /// </summary>
    public DefaultSettings? Parent { get; set; }

    /// <inheritdoc />
    public int LineNumber { get; set; }

    /// <summary/>
    public int LinePosition { get; set; }

    public bool HasLineInfo() => throw new NotImplementedException();
}

/// <summary>
/// Represents a free-format data structure.
/// </summary>
public sealed class TreeElement : Dictionary<string, TreeElement>;
