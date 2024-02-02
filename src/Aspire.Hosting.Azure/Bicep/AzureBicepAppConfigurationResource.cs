// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

public class AzureBicepAppConfigurationResource(string name) :
    AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.appconfig.bicep"),
    IResourceWithConnectionString
{
    public string? GetConnectionString()
    {
        return Outputs["appConfigEndpoint"];
    }
}

public static class AzureBicepAppConfigurationExtensions
{
    public static IResourceBuilder<AzureBicepAppConfigurationResource> AddBicepAppConfiguration(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureBicepAppConfigurationResource(name)
        {
            ConnectionStringTemplate = $"{{{name}.outputs.appConfigEndpoint}}"
        };

        return builder.AddResource(resource)
                .AddParameter("configName", resource.CreateBicepResourceName())
                .AddParameter(AzureBicepResource.KnownParameters.PrincipalId)
                .AddParameter(AzureBicepResource.KnownParameters.PrincipalType)
                .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
