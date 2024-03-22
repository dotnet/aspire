// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;
using Aspire.Hosting.AWS.CloudFormation;

namespace Aspire.Hosting.AWS.CDK;

/// <summary>
///
/// </summary>
public interface IStackResource : ICloudFormationResource
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    Stack BuildStack(App app);
}

/// <summary>
///
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IStackResource<T> : IStackResource
    where T : Stack
{
    /// <summary>
    ///
    /// </summary>
    StackBuilderDelegate<T> StackBuilder { get; }
}
