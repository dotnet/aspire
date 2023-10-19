// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azurite;

public class AzuriteContainerResource(string name) : ContainerResource(name), IAzuriteResource
{
    public string GetConnectionString()
    {
        if (!this.TryGetAnnotationsOfType<AllocatedEndpointAnnotation>(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException("Azurite resource does not have endpoint annotation.");
        }

        var endpoints = allocatedEndpoints
            .ToDictionary(x => x.Name, x => x.Port);

        var blobPort = endpoints["blob"];
        var queuePort = endpoints["queue"];
        var tablePort = endpoints["table"];

        if (blobPort == 10000 && queuePort == 10001 && tablePort == 10002)
        {
            return "UseDevelopmentStorage=true";
        }
        else
        {
            return "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;"+
                   "AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;"+
                   $"BlobEndpoint=http://127.0.0.1:{blobPort}/devstoreaccount1;"+
                   $"QueueEndpoint=http://127.0.0.1:{queuePort}/devstoreaccount1;"+
                   $"TableEndpoint=http://127.0.0.1:{tablePort}/devstoreaccount1;";
        }
    }
}
