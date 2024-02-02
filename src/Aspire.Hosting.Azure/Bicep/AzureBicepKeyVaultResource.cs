// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

public class AzureBicepKeyVaultResource(string name) :
    AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.keyvault.bicep"),
    IResourceWithConnectionString
{
    public string? GetConnectionString()
    {
        return Outputs["vaultUri"];
    }
}

public static class AzureBicepKeyVaultResourceExtensions
{
    public static IResourceBuilder<AzureBicepKeyVaultResource> AddBicepKeyVault(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureBicepKeyVaultResource(name)
        {
            ConnectionStringTemplate = $"{{{name}.outputs.vaultUri}}"
        };

        return builder.AddResource(resource)
                    .AddParameter(AzureBicepResource.KnownParameters.PrincipalId)
                    .AddParameter(AzureBicepResource.KnownParameters.PrincipalType)
                    .AddParameter("vaultName", resource.CreateBicepResourceName())
                    .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
