// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Keycloak container.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class KeycloakContainerResource(string name) : ContainerResource(name), IResourceWithEnvironment, IResourceWithServiceDiscovery
{
}
