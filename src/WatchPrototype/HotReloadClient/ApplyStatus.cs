// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.DotNet.HotReload;

internal enum ApplyStatus
{
    /// <summary>
    /// Failed to apply updates.
    /// </summary>
    Failed = 0,

    /// <summary>
    /// All requested updates have been applied successfully.
    /// </summary>
    AllChangesApplied = 1,

    /// <summary>
    /// Succeeded aplying changes, but some updates were not applicable to the target process because of required capabilities.
    /// </summary>
    SomeChangesApplied = 2,

    /// <summary>
    /// No updates were applicable to the target process because of required capabilities.
    /// </summary>
    NoChangesApplied = 3,
}
