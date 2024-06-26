// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

internal class DockerfileBuildAnnotation(string contextPath, string dockerfilePath, string? stage) : IResourceAnnotation
{
    public string ContextPath => contextPath;
    public string DockerfilePath = dockerfilePath;
    public string? Stage => stage;
    public Dictionary<string, object> BuildArguments { get; } = new();
    public Dictionary<string, object> BuildSecrets { get; } = new();
}
