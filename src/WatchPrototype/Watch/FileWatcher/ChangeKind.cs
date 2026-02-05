// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.ExternalAccess.HotReload.Api;

namespace Microsoft.DotNet.Watch;

internal enum ChangeKind
{
    Update,
    Add,
    Delete
}

internal static class ChangeKindExtensions
{
    public static HotReloadFileChangeKind Convert(this ChangeKind changeKind) =>
        changeKind switch
        {
            ChangeKind.Update => HotReloadFileChangeKind.Update,
            ChangeKind.Add => HotReloadFileChangeKind.Add,
            ChangeKind.Delete => HotReloadFileChangeKind.Delete,
            _ => throw new InvalidOperationException()
        };
}

internal readonly record struct ChangedFile(FileItem Item, ChangeKind Kind);

internal readonly record struct ChangedPath(string Path, ChangeKind Kind);
