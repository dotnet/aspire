// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the configuration settings for client IP-based session affinity in a Kubernetes resource.
/// </summary>
/// <remarks>
/// This class encapsulates settings that define the behavior of session affinity
/// when based on the client's IP address. It is used within the broader session affinity configuration.
/// </remarks>
[YamlSerializable]
public sealed class ClientIPConfigV1
{
    /// <summary>
    /// Gets or sets the timeout duration, in seconds, for retaining client IP connections.
    /// Represents the time period after which idle client connections will time out.
    /// Nullable to indicate that no specific timeout value is set.
    /// </summary>
    [YamlMember(Alias = "timeoutSeconds")]
    public int? TimeoutSeconds { get; set; }
}
