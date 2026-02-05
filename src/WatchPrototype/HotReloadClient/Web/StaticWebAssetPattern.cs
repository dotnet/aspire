// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.DotNet.HotReload;

internal sealed partial class StaticWebAssetPattern(string directory, string pattern, string baseUrl)
{
    public string Directory { get; } = directory;
    public string Pattern { get; } = pattern;
    public string BaseUrl { get; } = baseUrl;
}
