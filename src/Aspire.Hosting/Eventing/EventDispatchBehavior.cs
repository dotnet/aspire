// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Eventing;

/// <summary>
/// Controls how events are dispatched to subscribers.
/// </summary>
public enum EventDispatchBehavior
{
    /// <summary>
    /// Fires events sequentially and blocks until they are all processed.
    /// </summary>
    BlockingSequential,

    /// <summary>
    /// Fires events concurrently and blocks until they are all processed.
    /// </summary>
    BlockingConcurrent,

    /// <summary>
    /// Fires events sequentially but does not block.
    /// </summary>
    NonBlockingSequential,

    /// <summary>
    /// Fires events concurrently but does not block.
    /// </summary>
    NonBlockingConcurrent
}
