// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a custom annotation that can be applied to a resource with varying methods of implementation depending on the consumer of the manifest.
///
/// Produces output such as `"custom": { "name": "value" }`.
/// </summary>
public sealed class CustomManifestOutputAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Name of the custom manifest annotation.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Value of the custom manifest annotation.
    /// </summary>
    public object Value { get; }

    /// <summary>
    /// The type of value being written.
    /// </summary>
    public JsonValueKind ValueKind { get; } = JsonValueKind.String;

    /// <summary>
    /// Create a custom annotation with the specified name and value, where the value is a string.
    /// </summary>
    /// <param name="name">Name of the annotation.</param>
    /// <param name="value">String value.</param>
    public CustomManifestOutputAnnotation(string name, string value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// Create a custom annotation with the specified name, value and type.
    /// </summary>
    /// <param name="name">Name of the annotation.</param>
    /// <param name="value">Value of the annotation.</param>
    /// <param name="valueKind">Type of the value to be output.</param>
    public CustomManifestOutputAnnotation(string name, object value, JsonValueKind valueKind)
    {
        Name = name;
        Value = value;
        ValueKind = valueKind;
    }
}

