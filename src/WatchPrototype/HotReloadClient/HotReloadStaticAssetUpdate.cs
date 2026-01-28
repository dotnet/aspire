// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.HotReload;

internal readonly struct HotReloadStaticAssetUpdate(string assemblyName, string relativePath, ImmutableArray<byte> content, bool isApplicationProject)
{
    public string RelativePath { get; } = relativePath;
    public string AssemblyName { get; } = assemblyName;
    public ImmutableArray<byte> Content { get; } = content;
    public bool IsApplicationProject { get; } = isApplicationProject;

    public static async ValueTask<HotReloadStaticAssetUpdate> CreateAsync(StaticWebAsset asset, CancellationToken cancellationToken)
    {
#if NET
        var blob = await File.ReadAllBytesAsync(asset.FilePath, cancellationToken);
#else
        var blob = File.ReadAllBytes(asset.FilePath);
#endif
        return new HotReloadStaticAssetUpdate(
            assemblyName: asset.AssemblyName,
            relativePath: asset.RelativeUrl,
            content: ImmutableCollectionsMarshal.AsImmutableArray(blob),
            asset.IsApplicationProject);
    }
}
