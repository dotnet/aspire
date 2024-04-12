// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;
using Aspire.Hosting.AWS.CloudFormation;

namespace Aspire.Hosting.AWS.CDK;

/// <summary>
///
/// </summary>
public interface IStackResource : ICloudFormationResource, IResourceWithConstruct
{
    /// <summary>
    ///
    /// </summary>
    Stack Stack { get; }
}
