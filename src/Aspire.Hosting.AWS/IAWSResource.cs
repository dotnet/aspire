// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS;

/// <summary>
/// Represents an AWS resource, as a marker interface for <see cref="IResource"/>'s.
/// </summary>
public interface IAWSResource : IResource
{
    /// <summary>
    /// Configuration for creating service clients from the AWS .NET SDK.
    /// </summary>
    IAWSSDKConfig? AWSSDKConfig { get; set; }

    /// <summary>
    /// Set by the AWSProvisioner to indicate the task that is provisioning the resource.
    /// </summary>
    public TaskCompletionSource? ProvisioningTaskCompletionSource { get; set; }
}
