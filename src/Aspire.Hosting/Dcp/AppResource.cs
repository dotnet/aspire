// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp.Model;
using System.Diagnostics;

namespace Aspire.Hosting.Dcp;

[DebuggerDisplay("DcpResourceName = {DcpResourceName}, DcpResourceKind = {DcpResourceKind}")]
internal class AppResource
{
    public CustomResource DcpResource { get; }
    public string DcpResourceName => DcpResource.Metadata.Name;
    public string DcpResourceKind => DcpResource.Kind;
    
    public AppResource(CustomResource dcpResource)
    {
        DcpResource = dcpResource;
    }

    public virtual List<ServiceAppResource> ServicesProduced { get; } = [];
}

internal class ServiceAppResource : AppResource
{
    public Service Service => (Service)DcpResource;
    public ServiceAppResource(Service service) : base(service)
    {
    }
    public override List<ServiceAppResource> ServicesProduced
    {
        get { throw new InvalidOperationException("Service resources do not produce any services"); }
    }
}   

[DebuggerDisplay("ModelResource = {ModelResource}, DcpResourceName = {DcpResourceName}, DcpResourceKind = {DcpResourceKind}")]
internal class RenderedModelResource : AppResource, IResourceReference
{
    public IResource ModelResource { get; }
    
    public RenderedModelResource(IResource modelResource, CustomResource dcpResource): base(dcpResource)
    {
        ModelResource = modelResource;
    }

    public new virtual List<ServiceWithModelResource> ServicesProduced { get; } = [];
    public virtual List<ServiceWithModelResource> ServicesConsumed { get; } = [];
}

internal sealed class ServiceWithModelResource : RenderedModelResource
{
    public Service Service => (Service)DcpResource;
    public EndpointAnnotation EndpointAnnotation { get; }

    public override List<ServiceWithModelResource> ServicesProduced
    {
        get { throw new InvalidOperationException("Service resources do not produce any services"); }
    }
    public override List<ServiceWithModelResource> ServicesConsumed
    {
        get { throw new InvalidOperationException("Service resources do not consume any services"); }
    }

    public ServiceWithModelResource(IResource modelResource, Service service, EndpointAnnotation sba) : base(modelResource, service)
    {
        EndpointAnnotation = sba;
    }
}

internal interface IResourceReference
{
    IResource ModelResource { get; }
    string DcpResourceName { get; }
}
