// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Globbing;

namespace Microsoft.DotNet.HotReload;

internal sealed partial class StaticWebAssetPattern
{
    public MSBuildGlob Glob => field ??= MSBuildGlob.Parse(Directory, Pattern);
}
