// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Azure;

public class AzureBicepStorageResource(string name) :
    AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.storage.bicep")
{

}

public class AzureBicepBlobStorageResource(string name, AzureBicepStorageResource storage) : Resource(name),
    IResourceWithConnectionString,
    IResourceWithParent<AzureBicepStorageResource>
{
    /// <summary>
    /// Gets the parent AzureStorageResource of this AzureBlobStorageResource.
    /// </summary>
    public AzureBicepStorageResource Parent => storage;

    /// <summary>
    /// Gets the connection string for the Azure Blob Storage resource.
    /// </summary>
    /// <returns>The connection string for the Azure Blob Storage resource.</returns>
    public string? GetConnectionString() => Parent.Outputs.TryGetValue("blobEndpoint", out var blobEndpoint) ? blobEndpoint : null;

    public void WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "azure.bicep.v0");
        context.Writer.WriteString("connectionString", $"{{{Parent.Name}.outputs.blobEndpoint}}");
        context.Writer.WriteString("parent", Parent.Name);
    }
}

public class AzureBicepTableStorageResource(string name, AzureBicepStorageResource storage) : Resource(name),
    IResourceWithConnectionString,
    IResourceWithParent<AzureBicepStorageResource>
{
    /// <summary>
    /// Gets the parent AzureStorageResource of this AzureBlobStorageResource.
    /// </summary>
    public AzureBicepStorageResource Parent => storage;

    /// <summary>
    /// Gets the connection string for the Azure Blob Storage resource.
    /// </summary>
    /// <returns>The connection string for the Azure Blob Storage resource.</returns>
    public string? GetConnectionString() => Parent.Outputs.TryGetValue("tableEndpoint", out var tableEndpoint) ? tableEndpoint : null;

    public void WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "azure.bicep.v0");
        context.Writer.WriteString("connectionString", $"{{{Parent.Name}.outputs.tableEndpoint}}");
        context.Writer.WriteString("parent", Parent.Name);
    }
}

public class AzureBicepQueueStorageResource(string name, AzureBicepStorageResource storage) : Resource(name),
    IResourceWithConnectionString,
    IResourceWithParent<AzureBicepStorageResource>
{
    /// <summary>
    /// Gets the parent AzureStorageResource of this AzureBlobStorageResource.
    /// </summary>
    public AzureBicepStorageResource Parent => storage;

    ///
    /// <summary>
    ///     Gets the connection string for the Azure Blob Storage resource.
    ///        </summary>
    ///          <returns>The connection string for the Azure Blob Storage resource.</returns>
    public string? GetConnectionString() => Parent.Outputs.TryGetValue("queueEndpoint", out var queueEndpoint) ? queueEndpoint : null;

    public void WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "azure.bicep.v0");
        context.Writer.WriteString("connectionString", $"{{{Parent.Name}.outputs.queueEndpoint}}");
        context.Writer.WriteString("parent", Parent.Name);
    }
}

public static class AzureBicepSqlResourceExtensions
{
    public static IResourceBuilder<AzureBicepStorageResource> AddAzureBicepStorage(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureBicepStorageResource(name);

        return builder.AddResource(resource)
                      .AddParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .AddParameter(AzureBicepResource.KnownParameters.PrincipalType)
                      .AddParameter("storageName", resource.CreateBicepResourceName())
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    public static IResourceBuilder<AzureBicepBlobStorageResource> AddBlob(this IResourceBuilder<AzureBicepStorageResource> builder, string name)
    {
        var resource = new AzureBicepBlobStorageResource(name, builder.Resource);

        return builder.ApplicationBuilder.AddResource(resource)
            .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    public static IResourceBuilder<AzureBicepTableStorageResource> AddTable(this IResourceBuilder<AzureBicepStorageResource> builder, string name)
    {
        var resource = new AzureBicepTableStorageResource(name, builder.Resource);

        return builder.ApplicationBuilder.AddResource(resource)
            .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    public static IResourceBuilder<AzureBicepQueueStorageResource> AddQueue(this IResourceBuilder<AzureBicepStorageResource> builder, string name)
    {
        var resource = new AzureBicepQueueStorageResource(name, builder.Resource);

        return builder.ApplicationBuilder.AddResource(resource)
            .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
