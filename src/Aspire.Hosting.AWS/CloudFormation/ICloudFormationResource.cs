// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS.CloudFormation;

/// <summary>
/// Resource representing an AWS CloudFormation stack.
/// </summary>
public interface ICloudFormationResource : IResource
{
    /// <summary>
    /// Configuration for creating service clients from the AWS .NET SDK.
    /// </summary>
    IAWSSDKConfig? AWSSDKConfig { get; set; }

    /// <summary>
    /// The configured Amazon CloudFormation service client used to make service calls. If this property set
    /// then AWSSDKConfig is ignored.
    /// </summary>
    IAmazonCloudFormation? CloudFormationClient { get; set; }

    /// <summary>
    /// The output parameters of the CloudFormation stack.
    /// </summary>
    List<Output>? Outputs { get; }

    /// <summary>
    /// The task completion source for the provisioning operation.
    /// </summary>
    TaskCompletionSource? ProvisioningTaskCompletionSource { get; set; }
}
