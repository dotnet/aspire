// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Managed;

/// <summary>
/// Provides the generated shared assembly manifest for managed server execution.
/// The checked-in partial type is completed at build time by a generated source file in the intermediate output folder.
/// </summary>
internal static partial class ServerSharedAssemblyManifest
{
    /// <summary>
    /// Gets the generated shared assembly names for managed server execution.
    /// </summary>
    internal static string[] GetSharedAssemblyNames()
    {
        List<string> names = [];
        PopulateSharedAssemblyNames(names);
        return [.. names];
    }

    // Implemented in obj\...\Generated\ServerSharedAssemblyManifest.g.cs during build.
    // The generated file is compiled into Aspire.Managed and is not checked into the repository.
    static partial void PopulateSharedAssemblyNames(List<string> names);
}
