// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp.Model;
using System.Diagnostics;

namespace Aspire.Hosting.Dcp;

[DebuggerDisplay("ModelResource = {ModelResource}, DcpResourceName = {DcpResourceName}")]
internal class AppResource : IResourceReference
{
    public IResource ModelResource { get; }
    public CustomResource DcpResource { get; }
    public string DcpResourceName => DcpResource.Metadata.Name;
    public virtual List<ServiceAppResource> ServicesProduced { get; } = [];
    public virtual List<ServiceAppResource> ServicesConsumed { get; } = [];

    public AppResource(IResource modelResource, CustomResource dcpResource)
    {
        ModelResource = modelResource;
        DcpResource = dcpResource;
    }
}

internal sealed class ServiceAppResource : AppResource
{
    public Service Service => (Service)DcpResource;
    public EndpointAnnotation EndpointAnnotation { get; }

    public override List<ServiceAppResource> ServicesProduced
    {
        get { throw new InvalidOperationException("Service resources do not produce any services"); }
    }
    public override List<ServiceAppResource> ServicesConsumed
    {
        get { throw new InvalidOperationException("Service resources do not consume any services"); }
    }

    public ServiceAppResource(IResource modelResource, Service service, EndpointAnnotation sba) : base(modelResource, service)
    {
        EndpointAnnotation = sba;
    }
}

internal interface IResourceReference
{
    IResource ModelResource { get; }
    string DcpResourceName { get; }
}
