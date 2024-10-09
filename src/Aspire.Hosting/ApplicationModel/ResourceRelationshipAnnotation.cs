// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

#pragma warning disable RS0016 // Add public types and members to the declared API
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class ResourceRelationshipAnnotation(IResource resource, string type) : IResourceAnnotation
{
    public IResource Resource { get; } = resource;

    public string Type { get; } = type;

    public Dictionary<string, object> Properties { get; } = new(StringComparers.ResourcePropertyName);
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore RS0016 // Add public types and members to the declared API
