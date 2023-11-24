// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Http Service connection.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class HttpServiceResource(string name) : Resource(name), IHttpServiceResource
{

}
