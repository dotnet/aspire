// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.DotNet.HotReload;

internal readonly struct StaticWebAsset(string filePath, string relativeUrl, string assemblyName, bool isApplicationProject)
{
    public string FilePath => filePath;
    public string RelativeUrl => relativeUrl;
    public string AssemblyName => assemblyName;
    public bool IsApplicationProject => isApplicationProject;

    public const string WebRoot = "wwwroot";
    public const string ManifestFileName = "staticwebassets.development.json";

    public static bool IsScopedCssFile(string filePath)
        => filePath.EndsWith(".razor.css", StringComparison.Ordinal) ||
           filePath.EndsWith(".cshtml.css", StringComparison.Ordinal);

    public static string GetScopedCssRelativeUrl(string applicationProjectFilePath, string containingProjectFilePath)
        => WebRoot + "/" + GetScopedCssBundleFileName(applicationProjectFilePath, containingProjectFilePath);

    public static string GetScopedCssBundleFileName(string applicationProjectFilePath, string containingProjectFilePath)
    {
        var sourceProjectName = Path.GetFileNameWithoutExtension(containingProjectFilePath);

        return string.Equals(containingProjectFilePath, applicationProjectFilePath, StringComparison.OrdinalIgnoreCase)
            ? $"{sourceProjectName}.styles.css"
            : $"{sourceProjectName}.bundle.scp.css";
    }

    public static bool IsScopedCssBundleFile(string filePath)
        => filePath.EndsWith(".bundle.scp.css", StringComparison.Ordinal) ||
           filePath.EndsWith(".styles.css", StringComparison.Ordinal);

    public static bool IsCompressedAssetFile(string filePath)
        => filePath.EndsWith(".gz", StringComparison.Ordinal);

    public static string? GetRelativeUrl(string applicationProjectFilePath, string containingProjectFilePath, string assetFilePath)
        => IsScopedCssFile(assetFilePath)
        ? GetScopedCssRelativeUrl(applicationProjectFilePath, containingProjectFilePath)
        : GetAppRelativeUrlFomDiskPath(containingProjectFilePath, assetFilePath);

    /// <summary>
    /// For non scoped css, the only static files which apply are the ones under the wwwroot folder in that project. The relative path
    /// will always start with wwwroot. eg:  "wwwroot/css/styles.css"
    /// </summary>
    public static string? GetAppRelativeUrlFomDiskPath(string containingProjectFilePath, string assetFilePath)
    {
        var webRoot = "wwwroot" + Path.DirectorySeparatorChar;
        var webRootDir = Path.Combine(Path.GetDirectoryName(containingProjectFilePath)!, webRoot);

        return assetFilePath.StartsWith(webRootDir, StringComparison.OrdinalIgnoreCase)
            ? assetFilePath.Substring(webRootDir.Length - webRoot.Length).Replace("\\", "/")
            : null;
    }
}
