// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Constructs;

namespace Aspire.Hosting.AWS.CDK;

/// <inheritdoc cref="IConstructResource"/>
public interface IConstructResource<out T> : IConstructResource, IResourceWithConstruct<T> where T : IConstruct;
