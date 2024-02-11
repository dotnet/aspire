// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Data.Cosmos;

namespace Aspire.Hosting.Azure;

public class AzureCosmosDBEmulatorResource(AzureCosmosDBResource innerResource) : ContainerResource(innerResource.Name), IResource
{
    private readonly AzureCosmosDBResource _innerResource = innerResource;

    public new string Name => _innerResource.Name;

    public new ResourceMetadataCollection Annotations => _innerResource.Annotations;
}
