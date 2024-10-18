// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public interface IDashboardClientStatus
{
    /// <summary>
    /// Gets whether the client object is enabled for use.
    /// </summary>
    /// <remarks>
    /// Users of <see cref="IDashboardClient"/> client should check <see cref="IsEnabled"/> before calling
    /// any other members of this interface, to avoid exceptions.
    /// </remarks>
    bool IsEnabled { get; }
}
