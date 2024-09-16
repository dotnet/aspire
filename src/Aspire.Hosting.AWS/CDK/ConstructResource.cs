// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Constructs;

namespace Aspire.Hosting.AWS.CDK;

/// <inheritdoc cref="IConstructResource" />
internal class ConstructResource(string name, IConstruct construct, IResourceWithConstruct parent) : Resource(name), IConstructResource
{
    /// <inheritdoc/>
    public IConstruct Construct { get; } = construct;

    /// <inheritdoc/>
    public IResourceWithConstruct Parent { get; } = parent;
}

/// <inheritdoc cref="ConstructResource" />
internal sealed class ConstructResource<T>(string name, T construct, IResourceWithConstruct parent) : ConstructResource(name, construct, parent), IConstructResource<T>
    where T : IConstruct
{
    public new T Construct { get; } = construct;
}
