// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Python;

// Marker annotation to indicate a resource is for setting up a Python environment.
internal class PythonEnvironmentAnnotation : IResourceAnnotation
{
    public string? Version { get; set; }

    public VirtualEnvironment? VirtualEnvironment { get; set; }
}
