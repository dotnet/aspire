// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Http Service resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="Uri"></param>
public class HttpServiceResource(string name, Uri Uri) : Resource(name)
{
    public Uri Uri { get; } = Uri;
}
