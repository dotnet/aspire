// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CloudFormation.Model;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS.CloudFormation;

/// <summary>
/// Resource representing an AWS CloudFormation stack.
/// </summary>
public interface ICloudFormationResource : IResource
{
    /// <summary>
    /// Path to the CloudFormation template.
    /// </summary>
    string TemplatePath { get; }

    /// <summary>
    /// The output parameters of the CloudFormation stack.
    /// </summary>
    List<Output>? Outputs { get; }
}
