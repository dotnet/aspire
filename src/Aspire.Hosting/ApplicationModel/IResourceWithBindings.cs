// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a resource that has bindings associated with it.
/// </summary>
public interface IResourceWithBindings : IResource
{
    /// <summary>
    /// Gets an endpoint reference for the specified binding.
    /// </summary>
    /// <param name="bindingName">The name of the binding.</param>
    /// <returns>An <see cref="EndpointReference"/> object representing the endpoint reference 
    /// for the specified binding.</returns>
    /// <exception cref="DistributedApplicationException">Throws an exception if the a binding with the name <paramref name="bindingName"/> does not exist on the specified resource.</exception>
    public EndpointReference GetEndpoint(string bindingName)
    {
        if (!Annotations.OfType<ServiceBindingAnnotation>().Any(a => a.Name == bindingName))
        {
            throw new DistributedApplicationException($"Service binding with name '{bindingName}' does not exist on the specified resource");
        }

        return new EndpointReference(this, bindingName);
    }
}
