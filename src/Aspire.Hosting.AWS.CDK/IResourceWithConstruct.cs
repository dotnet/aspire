// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Constructs;

namespace Aspire.Hosting.AWS.CDK;

/// <summary>
///
/// </summary>
public interface IResourceWithConstruct : IResource
{
    /// <summary>
    ///
    /// </summary>
    Construct Construct { get; }
}

/// <summary>
///
/// </summary>
public interface IResourceWithConstruct<out T> : IResourceWithConstruct
    where T : Construct
{
    /// <summary>
    ///
    /// </summary>
    new T Construct { get; }
}
