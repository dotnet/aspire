// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// An annotation which represents the relationship between two resources.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, Resource = {Resource.Name}, RelationshipType = {Type}")]
public sealed class ResourceRelationshipAnnotation(IResource resource, string type) : IResourceAnnotation
{
    /// <summary>
    /// The resource that the relationship is to.
    /// </summary>
    public IResource Resource { get; } = resource;

    /// <summary>
    /// The relationship type.
    /// </summary>
    public string Type { get; } = type;
}
