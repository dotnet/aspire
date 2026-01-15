// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.JavaScript;

/// <summary>
/// Represents an annotation that specifies the output file/directory paths to be included in the final image.
/// </summary>
public sealed class JavaScriptBuildOutputAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets the output paths produced by the build process.
    /// </summary>
    public string[] Paths { get; }

    /// <param name="paths">An array of paths produced by the build process.</param>
    public JavaScriptBuildOutputAnnotation(string[] paths)
    {
        for (var i = 0; i < paths.Length; i++)
        {
            ArgumentException.ThrowIfNullOrEmpty(paths[i]);

            if (paths[i].StartsWith('/'))
            {
                throw new ArgumentException("Build output paths cannot be absolute path.", nameof(paths));
            }
        }

        Paths = paths;
    }
}
