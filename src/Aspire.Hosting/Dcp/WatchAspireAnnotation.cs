// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dcp;

internal sealed class WatchAspireAnnotation(
    string serverPipeName,
    string statusPipeName,
    string controlPipeName,
    Dictionary<string, IResource> projectPathToResource) : IResourceAnnotation
{
    public string ServerPipeName { get; } = serverPipeName;
    public string StatusPipeName { get; } = statusPipeName;
    public string ControlPipeName { get; } = controlPipeName;
    public Dictionary<string, IResource> ProjectPathToResource { get; } = projectPathToResource;
}
