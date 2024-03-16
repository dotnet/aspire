// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an entry in the metadata property of a generated manifest. 
/// </summary>
/// <param name="name"></param>
/// <param name="value"></param>
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
