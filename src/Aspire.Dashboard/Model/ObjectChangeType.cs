// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public enum ObjectChangeType
{
    Other,

    /// <summary>
    /// The object was added if new, or updated if not.
    /// </summary>
    Upsert,

    /// <summary>
    /// The object was deleted.
    /// </summary>
    Deleted
}
