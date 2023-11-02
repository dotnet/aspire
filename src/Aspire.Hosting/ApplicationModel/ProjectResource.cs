// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a project resource that implements <see cref="IResourceWithEnvironment"/> and 
/// <see cref="IResourceWithBindings"/>.
/// </summary>
public class ProjectResource(string name) : Resource(name), IResourceWithEnvironment, IResourceWithBindings
{
}
