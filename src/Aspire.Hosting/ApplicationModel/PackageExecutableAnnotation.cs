// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

internal sealed class PackageExecutableAnnotation : IResourceAnnotation
{
    public required string PackageId { get; set; }

    public string? Version { get; set; }

    public string? ExecutableName { get; set; }

    public string? WorkingDirectory { get; set; }

    public List<string> Sources { get; } = [];

    public bool IgnoreExistingFeeds { get; set; }

    public bool IgnoreFailedSources { get; set; }
}