// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

namespace Aspire.Hosting.AWS.CloudFormation;

/// <summary>
/// Resource representing an AWS CloudFormation stack.
/// </summary>
public interface ICloudFormationResource : IAWSResource
{
    /// <summary>
    /// The configured Amazon CloudFormation service client used to make service calls. If this property set
    /// then AWSSDKConfig is ignored.
    /// </summary>
    IAmazonCloudFormation? CloudFormationClient { get; set; }

    /// <summary>
    /// The name of the Amazon CloudFormation stack
    /// </summary>
    string StackName { get; }

    /// <summary>
    /// The output parameters of the CloudFormation stack.
    /// </summary>
    List<Output>? Outputs { get; }
}
