// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.ServiceBus.ApplicationModel;

/// <summary>
/// Entity status.
/// </summary>
public enum ServiceBusMessagingEntityStatus
{
    /// <summary>
    /// Unknown.
    /// </summary>
    Unknown,

    /// <summary>
    /// Active.
    /// </summary>
    Active,

    /// <summary>
    /// Disabled.
    /// </summary>
    Disabled,

    /// <summary>
    /// Restoring.
    /// </summary>
    Restoring,

    /// <summary>
    /// SendDisabled.
    /// </summary>
    SendDisabled,

    /// <summary>
    /// ReceiveDisabled.
    /// </summary>
    ReceiveDisabled,

    /// <summary>
    /// Creating.
    /// </summary>
    Creating,

    /// <summary>
    /// Deleting.
    /// </summary>
    Deleting,

    /// <summary>
    /// Renaming.
    /// </summary>
    Renaming,
}
