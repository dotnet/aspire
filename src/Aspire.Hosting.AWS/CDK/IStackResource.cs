// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;
using Aspire.Hosting.AWS.CloudFormation;

namespace Aspire.Hosting.AWS.CDK;

/// <summary>
/// Resource representing an AWS CDK stack.
/// </summary>
public interface IStackResource : ICloudFormationTemplateResource, IResourceWithConstruct
{
    /// <summary>
    /// The AWS CDK stack
    /// </summary>
    Stack Stack { get; }
}
