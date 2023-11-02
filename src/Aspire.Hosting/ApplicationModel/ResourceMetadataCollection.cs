// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a collection of resource metadata annotations.
/// </summary>
public sealed class ResourceMetadataCollection : Collection<IResourceAnnotation>
{
}
