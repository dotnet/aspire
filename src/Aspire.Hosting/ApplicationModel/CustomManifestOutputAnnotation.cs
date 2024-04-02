// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a custom annotation that can be applied to a resource with varying methods of implementation depending on the consumer of the manifest.
///
/// Produces output such as `"name": [
///     "value"
/// ]`.
/// </summary>
public class CustomManifestOutputAnnotation : IResourceAnnotation
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
    /// Create a custom annotation with the specified name and value.
    /// </summary>
    /// <param name="name">Name of the annotation.</param>
    /// <param name="value">Value to be serialized and output.</param>
    public CustomManifestOutputAnnotation(string name, object value)
    {
        Name = name;
        Value = value;
    }
}

