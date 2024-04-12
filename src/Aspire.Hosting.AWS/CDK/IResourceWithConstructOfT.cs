// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Constructs;

namespace Aspire.Hosting.AWS.CDK;

/// <summary>
///
/// </summary>
public interface IResourceWithConstruct<out T> : IResourceWithConstruct
    where T : IConstruct
{
    /// <summary>
    ///
    /// </summary>
    new T Construct { get; }
}
