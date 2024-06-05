// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Milvus;

/// <summary>
/// A resource that represents an Attu container for Milvus administration.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AttuResource(string name) : ContainerResource(name)
{
}
