// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp.Model;
//using System.Collections.Generic;
//using k8s.Models;

namespace Aspire.Hosting.Dcp;

internal sealed class DcpResourceState(Dictionary<string, IResource> applicationModel, List<AppResource> appResources)
{
    public readonly ConcurrentDictionary<string, Container> ContainersMap = [];
    public readonly ConcurrentDictionary<string, Executable> ExecutablesMap = [];
    public readonly ConcurrentDictionary<string, ContainerExec> ContainerExecsMap = [];
    public readonly ConcurrentDictionary<string, Service> ServicesMap = [];
    public readonly ConcurrentDictionary<string, Endpoint> EndpointsMap = [];
    public readonly ConcurrentDictionary<(string, string), List<string>> ResourceAssociatedServicesMap = [];

    public Dictionary<string, IResource> ApplicationModel { get; } = applicationModel;
    public List<AppResource> AppResources { get; } = appResources;

    public void AddResource(AppResource appResource)
    {
        var modelResource = appResource.ModelResource;
        //var dcpResource = appResource.DcpResource;

        ApplicationModel.TryAdd(modelResource.Name, modelResource);
        if (!AppResources.Contains(appResource))
        {
            AppResources.Add(appResource);
        }

        //_ = appResource.DcpResource switch
        //{
        //    Container c => ContainersMap.TryAdd(dcpResource.Name(), c),
        //    _ => false
        //};
    }
}
