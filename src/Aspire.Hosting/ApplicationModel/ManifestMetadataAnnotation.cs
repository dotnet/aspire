// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an entry in the metadata property of a generated manifest. 
/// </summary>
/// <param name="name">The name of the metadata entry in the manifest.</param>
/// <param name="value">The value of the metadata entry in the manifest.</param>
/// <remarks>
///     <para>
///         The <see cref="ManifestMetadataAnnotation.Value"/> will be serialized using the <see cref="System.Text.Json.JsonSerializer"/>
///         using the default serialization options.
///     </para>
/// </remarks>
public class ManifestMetadataAnnotation(string name, object value) : IResourceAnnotation
{
    /// <summary>
    /// Name of metadata entry.
    /// </summary>
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));

    /// <summary>
    /// Value of metadata entry.
    /// </summary>
    public object Value { get; } = value ?? throw new ArgumentNullException(nameof(value));
}
