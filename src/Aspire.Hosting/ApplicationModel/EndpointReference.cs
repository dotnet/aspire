// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

public sealed class EndpointReference(IResourceWithBindings owner, string bindingName)
{
    public IResourceWithBindings Owner { get; } = owner;
    public string BindingName { get; } = bindingName;

    public string UriString
    {
        get
        {
            var allocatedEndpoint = Owner.Annotations.OfType<AllocatedEndpointAnnotation>().SingleOrDefault(a => a.Name == BindingName);
            return allocatedEndpoint?.UriString ?? $"{{{Owner.Name}.bindings.{BindingName}.url}}";
        }
    }
}
