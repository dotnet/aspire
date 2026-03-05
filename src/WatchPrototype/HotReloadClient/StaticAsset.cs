// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.DotNet.HotReload;

internal readonly struct StaticAsset(string filePath, string relativeUrl, string assemblyName, bool isApplicationProject)
{
    public string FilePath => filePath;
    public string RelativeUrl => relativeUrl;
    public string AssemblyName => assemblyName;
    public bool IsApplicationProject => isApplicationProject;
}
