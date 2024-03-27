// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Constructs;

namespace Aspire.Hosting.AWS.CDK;

/// <summary>
///
/// </summary>
public interface IConstructResource : IResourceWithParent<IResourceWithConstruct>, IResourceWithConstruct;

/// <summary>
///
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IConstructResource<out T> : IConstructResource, IResourceWithConstruct<T> where T : Construct;
