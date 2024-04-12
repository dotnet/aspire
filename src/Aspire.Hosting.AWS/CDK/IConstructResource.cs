// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS.CDK;

/// <summary>
///
/// </summary>
public interface IConstructResource : IResourceWithParent<IResourceWithConstruct>, IResourceWithConstruct;
