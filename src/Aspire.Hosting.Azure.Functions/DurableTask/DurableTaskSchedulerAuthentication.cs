// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents the authentication methods supported by the Durable Task Scheduler.
/// </summary>
public static class DurableTaskSchedulerAuthentication
{
    /// <summary>
    /// No authentication is used with the scheduler.
    /// </summary>
    /// <remarks>
    /// This is suitable only for local, emulator-based development.
    /// </remarks>
    public const string None = "None";

    /// <summary>
    /// Use the developer's Azure account to authenticate with the scheduler.
    /// </summary>
    public const string Default = "DefaultAzure";

    /// <summary>
    /// Use managed identity to authenticate with the scheduler.
    /// </summary>
    public const string ManagedIdentity = "ManagedIdentity";
}
