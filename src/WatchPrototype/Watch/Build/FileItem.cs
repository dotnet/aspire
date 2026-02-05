// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Watch
{
    internal readonly record struct FileItem
    {
        public required string FilePath { get; init; }

        /// <summary>
        /// List of all projects that contain this file (does not contain duplicates).
        /// Empty if the item is added but not been assigned to a project yet.
        /// </summary>
        public required List<string> ContainingProjectPaths { get; init; }

        public string? StaticWebAssetRelativeUrl { get; init; }
    }
}
