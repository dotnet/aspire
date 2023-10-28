// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Otlp.Storage;

/// <summary>
/// Indicates the purpose of the subscription.
/// </summary>
public enum SubscriptionType
{
    /// <summary>
    /// On subscription notification the app will read the latest data.
    /// </summary>
    Read,
    /// <summary>
    /// On subscription notification the app won't read the latest data.
    /// </summary>
    Other
}
