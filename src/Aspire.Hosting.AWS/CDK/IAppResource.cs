// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;

namespace Aspire.Hosting.AWS.CDK;

/// <summary>
/// Resource representing an AWS CDK app.
/// </summary>
public interface IAppResource : IResourceWithConstruct
{
    /// <summary>
    /// The AWS CDK app.
    /// </summary>
    App App { get; }
}
