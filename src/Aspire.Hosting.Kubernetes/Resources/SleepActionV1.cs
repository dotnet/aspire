// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents an action to pause execution for a specified duration in seconds.
/// </summary>
/// <remarks>
/// SleepActionV1 is primarily used in Kubernetes lifecycle handlers
/// to introduce a delay or pause during specific points of the Pod's lifecycle,
/// such as during pre-stop or post-start events.
/// </remarks>
[YamlSerializable]
public sealed class SleepActionV1
{
    /// <summary>
    /// Represents the duration in seconds associated with a sleep action.
    /// </summary>
    /// <remarks>
    /// The value specifies the number of seconds the action should pause or delay.
    /// </remarks>
    [YamlMember(Alias = "seconds")]
    public long Seconds { get; set; }
}
