// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Watch;

internal enum ChangeKind
{
    Update,
    Add,
    Delete
}

internal readonly record struct ChangedFile(FileItem Item, ChangeKind Kind);

internal readonly record struct ChangedPath(string Path, ChangeKind Kind);
