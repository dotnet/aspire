// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

internal sealed class ResolvedPackageExecutableAnnotation : IResourceAnnotation
{
    public required string PackageId { get; set; }

    public required string PackageVersion { get; set; }

    public required string PackageDirectory { get; set; }

    public required string ExecutablePath { get; set; }

    public required string Command { get; set; }

    public required string WorkingDirectory { get; set; }

    public List<string> Arguments { get; } = [];
}