// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// An annotation that specifies the default launch profile for a resource.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, LaunchProfileName = {LaunchProfileName}")]
public sealed class DefaultLaunchProfileAnnotation(string launchProfileName) : IResourceAnnotation
{
    private readonly string _launchProfileName = launchProfileName ?? throw new ArgumentNullException(nameof(launchProfileName));

    /// <summary>
    /// The name of the default launch profile.
    /// </summary>
    public string LaunchProfileName => _launchProfileName;
}
