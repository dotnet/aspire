// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.AWS.CloudFormation;

/// <inheritdoc cref="Aspire.Hosting.AWS.CloudFormation.ICloudFormationResource" />
internal abstract class CloudFormationResource(string name, string stackName) : Resource(name), ICloudFormationResource
{
    public string StackName { get; } = stackName;

    /// <inheritdoc/>
    public IAWSSDKConfig? AWSSDKConfig { get; set; }

    /// <inheritdoc/>
    public IAmazonCloudFormation? CloudFormationClient { get; set; }

    /// <inheritdoc/>
    public List<Output>? Outputs { get; set; }

    /// <inheritdoc/>
    public TaskCompletionSource? ProvisioningTaskCompletionSource { get; set; }

    internal abstract void WriteToManifest(ManifestPublishingContext context);
}
