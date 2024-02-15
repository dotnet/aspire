// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Data.Cosmos;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Wraps an <see cref="AzureCosmosDBResource" /> in a type that exposes container extension methods.
/// </summary>
/// <param name="innerResource">The inner resource used to store annotations.</param>
public class AzureCosmosDBEmulatorResource(AzureCosmosDBResource innerResource) : ContainerResource(innerResource.Name), IResource
{
    private readonly AzureCosmosDBResource _innerResource = innerResource;

    /// <inheritdoc/>
    public override string Name => _innerResource.Name;

    /// <summary>
    /// TODO: Doc Comments
    /// </summary>
    public override ResourceMetadataCollection Annotations => _innerResource.Annotations;
}
