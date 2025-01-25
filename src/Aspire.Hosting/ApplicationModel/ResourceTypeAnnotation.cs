// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents the kind of a resource.
/// </summary>
/// <param name="resourceKind"></param>
public class ResourceTypeAnnotation(Type resourceKind) : IResourceAnnotation
{
    /// <summary>
    /// The kind of the resource.
    /// </summary>
    public Type ResourceKind { get; } = resourceKind;
}