// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

public class AzureBicepRedisResource(string name) :
    AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.redis.bicep"),
    IResourceWithConnectionString
{
    public string ResourceNameOutputKey => "cacheName";

    public string AccountKeyOutputKey => "accountKey";

    public string? GetConnectionString()
    {
        return $"{Outputs["hostName"]},ssl=true,password={Outputs[AccountKeyOutputKey]}";
    }
}

public static class AzureBicepRedisExtensions
{
    public static IResourceBuilder<AzureBicepRedisResource> AddBicepRedis(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureBicepRedisResource(name)
        {
            ConnectionStringTemplate = $"{{{name}.outputs.hostName}},ssl=true,password={{keys({{Microsoft.Cache/redis@2023-04-15/{name}.outputs.cacheName}})}}"
        };

        return builder.AddResource(resource)
                    .AddParameter("redisCacheName", resource.CreateBicepResourceName())
                    .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
