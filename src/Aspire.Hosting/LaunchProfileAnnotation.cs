// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that specifies the launch profile name for a resource.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, LaunchProfileName = {LaunchProfileName}")]
public sealed class LaunchProfileAnnotation(string launchProfileName) : IResourceAnnotation
{
    private readonly string _launchProfileName = launchProfileName ?? throw new ArgumentNullException(nameof(launchProfileName));

    /// <summary>
    /// Gets the launch profile name.
    /// </summary>
    public string LaunchProfileName => _launchProfileName;
}
