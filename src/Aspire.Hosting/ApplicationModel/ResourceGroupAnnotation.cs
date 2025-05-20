// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A collection of resources logically grouped together that will appear grouped in the Aspire Dashboard.
/// </summary>
[DebuggerDisplay("Name = {Name}")]
public sealed class ResourceGroupAnnotation : IResourceAnnotation
{
    /// <summary>
    /// The group name. Must be unique.
    /// </summary>
    public required string Name { get; init; }
}
