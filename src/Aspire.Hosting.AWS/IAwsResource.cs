// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an AWS resource, as a marker interface for <see cref="IResource"/>'s
/// that can be deployed to an AWS. And provides the Amazon Resource Name (ARN) of the resource.
/// </summary>
public interface IAwsResource : IResource
{
    /// <summary>
    ///  Gets the Amazon Resource Name (ARN) of the resource.
    /// </summary>
    string? Arn { get; }
}
