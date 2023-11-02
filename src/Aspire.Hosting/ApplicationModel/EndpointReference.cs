// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an endpoint reference for a resource with bindings.
/// </summary>
/// <param name="owner">The resource with bindings that owns the endpoint reference.</param>
/// <param name="bindingName">The name of the binding.</param>
public sealed class EndpointReference(IResourceWithBindings owner, string bindingName)
{
    /// <summary>
    /// Gets the owner of the endpoint reference.
    /// </summary>
    public IResourceWithBindings Owner { get; } = owner;

    /// <summary>
    /// Gets the name of the binding associated with the endpoint reference.
    /// </summary>
    public string BindingName { get; } = bindingName;

    /// <summary>
    /// Gets the URI string for the endpoint reference.
    /// </summary>
    public string UriString
    {
        get
        {
            var allocatedEndpoint = Owner.Annotations.OfType<AllocatedEndpointAnnotation>().SingleOrDefault(a => a.Name == BindingName);
            return allocatedEndpoint?.UriString ?? $"{{{Owner.Name}.bindings.{BindingName}.url}}";
        }
    }
}
