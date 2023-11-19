// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CloudFormation.Constructs;

namespace Aspire.Hosting.AWS.Provisioning;

internal sealed class ProvisioningContext()
{
    // public RegionEndpoint Location => location;
}

internal interface IAwsResourceProvisioner
{
    void ConfigureResource(IConfiguration configuration, IAwsResource resource);

    IAwsConstruct CreateConstruct(IAwsResource resource, ProvisioningContext context);
}

internal abstract class AwsResourceProvisioner<TResource, TConstruct> : IAwsResourceProvisioner
    where TResource : class, IAwsResource
    where TConstruct : AwsConstruct
{
    public abstract void ConfigureResource(IConfiguration configuration, TResource resource);

    public abstract TConstruct CreateConstruct(TResource resource, ProvisioningContext context);

    public void ConfigureResource(IConfiguration configuration, IAwsResource resource) =>
        ConfigureResource(configuration, (TResource)resource);

    public IAwsConstruct CreateConstruct(IAwsResource resource, ProvisioningContext context) =>
        CreateConstruct((TResource)resource, context);
}
