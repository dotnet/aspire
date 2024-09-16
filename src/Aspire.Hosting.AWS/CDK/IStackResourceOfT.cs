// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;

namespace Aspire.Hosting.AWS.CDK;

/// <inheritdoc cref="IStackResource"/>
public interface IStackResource<out T> : IStackResource, IResourceWithConstruct<T>
    where T : Stack
{
    /// <summary>
    /// The AWS CDK stack
    /// </summary>
    new T Stack { get; }
}
