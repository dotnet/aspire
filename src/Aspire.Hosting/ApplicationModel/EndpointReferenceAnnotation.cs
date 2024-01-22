// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

[DebuggerDisplay(@"Type = {GetType().Name,nq}, Resource = {Resource.Name}, EndpointNames = {string.Join("", "", EndpointNames)}")]
internal sealed class EndpointReferenceAnnotation(IResource resource) : IResourceAnnotation
{
    public IResource Resource { get; } = resource;
    public bool UseAllEndpoints { get; set; }
    public Collection<string> EndpointNames { get; } = new();
}
