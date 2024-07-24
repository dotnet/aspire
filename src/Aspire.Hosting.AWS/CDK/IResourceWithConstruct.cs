// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Constructs;

namespace Aspire.Hosting.AWS.CDK;

/// <summary>
/// Represents a resource that has an AWS CDK construct.
/// </summary>
public interface IResourceWithConstruct : IResource
{
    /// <summary>
    /// The AWS CDK construct
    /// </summary>
    IConstruct Construct { get; }
}
