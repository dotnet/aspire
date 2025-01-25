// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Represents a resource that is not managed by Aspire's provisioning or
/// container management layer.
/// </summary>
public class ExistingResourceAnnotation(string name, bool isPublishMode = false) : IResourceAnnotation
{
    /// <summary>
    /// The name of the existing resource.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// True if the existing resource is reference in publish mode.
    /// </summary>
    public bool IsPublishMode { get; } = isPublishMode;
}
