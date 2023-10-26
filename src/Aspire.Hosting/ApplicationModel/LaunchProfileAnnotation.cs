// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

[DebuggerDisplay("Type = {GetType().Name,nq}, LaunchProfileName = {LaunchProfileName}")]
internal sealed class LaunchProfileAnnotation : IResourceAnnotation
{
    public LaunchProfileAnnotation(string launchProfileName, LaunchProfile launchProfile)
    {
        LaunchProfileName = launchProfileName;
        LaunchProfile = launchProfile;
    }

    public string LaunchProfileName { get; }

    public LaunchProfile LaunchProfile { get; }
}
