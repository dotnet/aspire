// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS.Lambda;

/// <summary>
///
/// </summary>
/// <seealso cref="IResourceWithEnvironment"/>
public interface ILambdaFunction : IResourceWithEnvironment, IResourceWithEndpoints
{
    /// <summary>
    ///
    /// </summary>
    LambdaRuntime Runtime { get; }
}
