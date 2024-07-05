// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;

namespace Aspire.Hosting.AWS.CDK;

/// <summary>
/// This resource is a <see cref="IStackResource"/>, but also contains the AWS CDK App, which will be used as root for
/// the stack and construct building. It is possible to have more than one and can contain additional stacks.
/// </summary>
public interface ICDKResource : IStackResource
{
    /// <summary>
    /// The AWS CDK App which hosts the stacks with constructs
    /// </summary>
    App App { get; }
}
