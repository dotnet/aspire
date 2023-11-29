// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Aspire.Hosting.Publishing;

public sealed class ManifestPublishingContext(string manifestPath, Utf8JsonWriter writer)
{
    public string ManifestPath { get; } = manifestPath;

    public Utf8JsonWriter Writer { get; } = writer;

    [return: NotNullIfNotNull(nameof(path))]
    public string? GetManifestRelativePath(string? path)
    {
        if (path is null)
        {
            return null;
        }

        var fullyQualifiedManifestPath = Path.GetFullPath(ManifestPath);
        var manifestDirectory = Path.GetDirectoryName(fullyQualifiedManifestPath) ?? throw new DistributedApplicationException("Could not get directory name of output path");
        var relativePath = Path.GetRelativePath(manifestDirectory, path);

        return relativePath;
    }
}
