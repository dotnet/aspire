// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp.Model;
using System.Diagnostics;

namespace Aspire.Hosting.Dcp;

[DebuggerDisplay("ModelResource = {ModelResource}, DcpResourceName = {DcpResourceName}, DcpResourceKind = {DcpResourceKind}")]
internal class AppResource
{
    public CustomResource DcpResource { get; }
    public string DcpResourceName => DcpResource.Metadata.Name;
    public string DcpResourceKind => DcpResource.Kind;
    public virtual List<ServiceAppResource> ServicesProduced { get; } = [];
    public virtual List<ServiceAppResource> ServicesConsumed { get; } = [];

    public AppResource(CustomResource dcpResource)
    {
        DcpResource = dcpResource;
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
}

internal sealed class ServiceAppResource : RenderedModelResource
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
