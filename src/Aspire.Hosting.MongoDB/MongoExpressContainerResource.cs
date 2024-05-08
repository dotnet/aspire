// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.MongoDB;

/// <summary>
/// A resource that represents a Mongo Express container.
/// </summary>
/// <param name="name">The name of the resource.</param>
public sealed class MongoExpressContainerResource(string name) : ContainerResource(name)
{
}
